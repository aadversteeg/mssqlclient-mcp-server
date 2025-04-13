param (
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "Creating version $Version..."

# Check if we are on master branch, if not switch to it
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "master") {
    Write-Host "Switching to master branch..."
    git checkout master
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to switch to master branch. Aborting."
        exit 1
    }
}

# Pull latest changes from master
Write-Host "Pulling latest changes from master..."
git pull origin master
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pull latest changes. Aborting."
    exit 1
}

# Create a tag with the version
Write-Host "Creating tag $Version..."
git tag -a "$Version" -m "Version $Version"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create tag. Aborting."
    exit 1
}

# Push the tag to remote
Write-Host "Pushing tag to remote..."
git push origin "$Version"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push tag to remote. Aborting."
    exit 1
}

Write-Host "Version $Version successfully created and pushed to remote."