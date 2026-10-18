<#
.SYNOPSIS
Open a GitHub issue for each new finding of a fuzzing session.

.DESCRIPTION
The issues are labeled "fuzzing" and contain the signature of their finding, so a finding is reported once:
- an open issue, or an issue closed as not planned or duplicate, means the finding is known, and nothing is done
- an issue closed as completed means the finding is a regression, and a new issue references it, unless the fuzzed commit
  does not contain the commit that closed the issue, as the finding is then known
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$FindingsDirectory,
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$CommitSha = $env:GITHUB_SHA,
    [string]$RunUrl,
    [int]$MaxIssues = 50,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$label = "fuzzing"

# The body of an issue is limited to 65536 characters
$maxInputLength = 20000
$maxDetailsLength = 12000

function Get-Fence([string]$text) {
    # A fence longer than any sequence of backticks of the content, so the content cannot close it
    $longest = 2
    foreach ($match in [regex]::Matches($text, '`+')) {
        $longest = [Math]::Max($longest, $match.Length)
    }

    return '`' * ($longest + 1)
}

function Format-CodeBlock([string]$text, [string]$language, [int]$maxLength) {
    $suffix = ""
    if ($text.Length -gt $maxLength) {
        $text = $text.Substring(0, $maxLength)
        $suffix = "`n_Truncated, the full content is in the fuzz-findings artifact of the run._`n"
    }

    $fence = Get-Fence $text
    return "$fence$language`n$($text.TrimEnd())`n$fence`n$suffix"
}

function Get-ExistingIssues([string]$signature) {
    $json = gh issue list --repo $Repository --label $label --state all --search "$signature in:body" --json "number,state,stateReason" --limit 20
    return @($json | ConvertFrom-Json)
}

function Get-ClosingCommit([int]$number) {
    # The merge commit of the pull request, or the commit, that closed the issue the last time
    $owner, $name = $Repository -split '/', 2
    $query = 'query($owner: String!, $name: String!, $number: Int!) { repository(owner: $owner, name: $name) { issue(number: $number) { timelineItems(itemTypes: [CLOSED_EVENT], last: 1) { nodes { ... on ClosedEvent { closer { ... on PullRequest { mergeCommit { oid } } ... on Commit { oid } } } } } } } }'
    $commit = gh api graphql -f query=$query -F owner=$owner -F name=$name -F number=$number --jq '.data.repository.issue.timelineItems.nodes[-1].closer | .mergeCommit.oid // .oid // empty'
    return [string]$commit
}

function Test-IsAncestor([string]$commit, [string]$descendant) {
    # git returns 1 when the commit is not an ancestor, and another code when a commit is unknown, for instance in a shallow clone
    $PSNativeCommandUseErrorActionPreference = $false
    git -C $PSScriptRoot merge-base --is-ancestor $commit $descendant 2>$null
    switch ($LASTEXITCODE) {
        0 { return $true }
        1 { return $false }
        default { return $null }
    }
}

function Get-IssueBody($finding, [string]$inputText, [string]$inputName, $regressionOf) {
    $lines = [System.Collections.Generic.List[string]]::new()
    if ($regressionOf) {
        $lines.Add("This finding was fixed in #$regressionOf, but it reproduces again.")
        $lines.Add("")
    }

    $source = "the [fuzz tests](https://github.com/$Repository/blob/main/tests/Meziantou.Analyzer.Fuzz/README.md)"
    if ($RunUrl) {
        $source += " ([run]($RunUrl))"
    }

    $lines.Add("This issue was found by $source.")
    $lines.Add("")
    $lines.Add("- Kind: ``$($finding.kind)``")
    if ($finding.diagnosticIds.Count -gt 0) {
        $lines.Add("- Rules: $(($finding.diagnosticIds | ForEach-Object { "[$_](https://github.com/$Repository/blob/main/docs/Rules/$_.md)" }) -join ', ')")
    }

    $lines.Add("- Component: ``$($finding.component)``")
    if ($finding.codeActionTitle) {
        $lines.Add("- Code action: ``$($finding.codeActionTitle)``")
    }

    if ($CommitSha) {
        $lines.Add("- Commit: $CommitSha")
    }

    $lines.Add("- Signature: ``$($finding.signature)``")
    $lines.Add("")
    $lines.Add("## Input")
    $lines.Add("")
    $lines.Add((Format-CodeBlock $inputText "cs" $maxInputLength))

    if ($finding.stackTrace) {
        $lines.Add("## Stack trace")
        $lines.Add("")
        $lines.Add((Format-CodeBlock $finding.stackTrace "" $maxDetailsLength))
    }
    elseif ($finding.message) {
        $lines.Add("## Details")
        $lines.Add("")
        $lines.Add((Format-CodeBlock $finding.message "" $maxDetailsLength))
    }

    if ($finding.fixedSource) {
        $lines.Add("## Code after the code fix")
        $lines.Add("")
        $lines.Add((Format-CodeBlock $finding.fixedSource "cs" $maxDetailsLength))
    }

    $lines.Add("## Reproduce")
    $lines.Add("")
    $lines.Add("Save the input as ``$inputName``, then run:")
    $lines.Add("")
    $lines.Add('```shell')
    $lines.Add("dotnet run --project tests/Meziantou.Analyzer.Fuzz --configuration Release -- repro $inputName")
    $lines.Add('```')
    $lines.Add("")
    $lines.Add("<!-- fuzz-signature: $($finding.signature) -->")
    return $lines -join "`n"
}

if (-not (Test-Path $FindingsDirectory)) {
    Write-Host "No finding"
    exit 0
}

$findings = @(Get-ChildItem $FindingsDirectory -Directory | ForEach-Object {
        $findingFile = Join-Path $_.FullName "finding.json"
        if (Test-Path $findingFile) {
            $minimized = Join-Path $_.FullName "minimized.cs"
            $inputFile = if (Test-Path $minimized) { Get-Item $minimized } else { Get-ChildItem $_.FullName -Filter "input-*.cs" | Sort-Object Length, Name | Select-Object -First 1 }
            if ($inputFile) {
                # The stack trace and the fixed code depend on the input, so they are read from the file of the input
                $inputFindingFile = [System.IO.Path]::ChangeExtension($inputFile.FullName, ".json")
                if (Test-Path $inputFindingFile) {
                    $findingFile = $inputFindingFile
                }

                [pscustomobject]@{
                    Finding   = Get-Content $findingFile -Raw | ConvertFrom-Json
                    InputFile = $inputFile
                }
            }
        }
    } | Sort-Object { $_.Finding.kind }, { $_.Finding.signature })

Write-Host "$($findings.Count) findings"
if (-not $DryRun) {
    gh label create $label --repo $Repository --color "D93F0B" --description "Found by the fuzz tests" --force
}

$createdCount = 0
foreach ($item in $findings) {
    $finding = $item.Finding

    # The exceptions of the harness or of Roslyn outside of the rules are rarely defects of the analyzers
    if ($finding.kind -in "RoslynException", "HarnessException" -or $finding.component -eq "unknown") {
        Write-Host "Skipping $($finding.signature): $($finding.title)"
        continue
    }

    $regressionOf = $null
    if ($Repository) {
        $existingIssues = Get-ExistingIssues $finding.signature
        $knownIssue = $existingIssues | Where-Object { $_.state -eq "OPEN" -or $_.stateReason -ne "COMPLETED" } | Select-Object -First 1
        if ($knownIssue) {
            Write-Host "Known in #$($knownIssue.number): $($finding.title)"
            continue
        }

        $fixedIssue = $existingIssues | Sort-Object number -Descending | Select-Object -First 1
        if ($fixedIssue) {
            # A commit older than the fix, such as a branch that is not up to date, still has the defect
            $closingCommit = Get-ClosingCommit $fixedIssue.number
            if ($closingCommit -and $CommitSha -and (Test-IsAncestor $closingCommit $CommitSha) -eq $false) {
                Write-Host "Fixed in #$($fixedIssue.number) by $closingCommit, which $CommitSha does not contain: $($finding.title)"
                continue
            }

            $regressionOf = $fixedIssue.number
        }
    }

    if ($createdCount -ge $MaxIssues) {
        Write-Host "Not reported, as $MaxIssues issues were already created: $($finding.title)"
        continue
    }

    $inputText = [System.IO.File]::ReadAllText($item.InputFile.FullName)
    $body = Get-IssueBody $finding $inputText "$($finding.signature).cs" $regressionOf
    if ($DryRun) {
        Write-Host "::group::$($finding.title)"
        Write-Host $body
        Write-Host "::endgroup::"
    }
    else {
        $bodyFile = New-TemporaryFile
        try {
            [System.IO.File]::WriteAllText($bodyFile.FullName, $body)
            gh issue create --repo $Repository --title $finding.title --body-file $bodyFile.FullName --label $label --label bug
        }
        finally {
            Remove-Item $bodyFile
        }
    }

    $createdCount++
}

Write-Host "$createdCount issues created"
