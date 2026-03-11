# Script to build, tag and push Unity Applicant Portal DB Migrator image for dev
# Make sure you're logged in first: 'oc login --web --server=https://api.silver.devops.gov.bc.ca:6443'
# Run from: .\Grants\applications\Grants.ApplicantPortal\src\Grants.ApplicantPortal.Backend

param(
    [string]$ImageTag = "latest"
)

$OC_REGISTRY = "image-registry.apps.silver.devops.gov.bc.ca"
$OC_TARGET_PROJECT = "d18498-dev"
$LOCAL_IMAGE_NAME = "applicant-portal-dbmigrator"
$IMAGESTREAM_NAME = "dev-applicantportal-dbmigrator"

# Build the Docker image
Write-Host "Building Docker image..." -ForegroundColor Green
docker build -f src\Grants.ApplicantPortal.API.Migrations\Dockerfile -t $LOCAL_IMAGE_NAME .

if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker build failed"
    exit 1
}

# Authenticate with OpenShift registry
Write-Host "Authenticating with OpenShift registry..." -ForegroundColor Green
oc registry login
docker login -u unused -p $(oc whoami -t) $OC_REGISTRY

if ($LASTEXITCODE -ne 0) {
    Write-Error "Registry authentication failed"
    exit 1
}

oc project $OC_TARGET_PROJECT

# Tag and push the image
$TARGET_IMAGE = "$OC_REGISTRY/$OC_TARGET_PROJECT/$IMAGESTREAM_NAME`:$ImageTag"
Write-Host "Tagging and pushing image: $TARGET_IMAGE" -ForegroundColor Green
docker tag $LOCAL_IMAGE_NAME $TARGET_IMAGE
docker push $TARGET_IMAGE

if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker push failed"
    exit 1
}

Write-Host "Successfully built, tagged and pushed: $TARGET_IMAGE" -ForegroundColor Green

# Clean and Run dbmigrator job from project template and params file
Write-Host "Running migration job..." -ForegroundColor Green
oc delete jobs dev-applicantportal-dbmigrator -n $OC_TARGET_PROJECT --ignore-not-found=true
oc process unity-applicantportal-dbmigrator-job -n $OC_TARGET_PROJECT -p APPLICATION_GROUP=dev-unity-applicant-portal -p DATABASE_SERVICE_NAME=dev-unity-applicant-portal -p APPLICATION_NAME=dev-applicantportal-dbmigrator -p IMAGEPULL_NAMESPACE=$OC_TARGET_PROJECT -p IMAGESTREAM_NAME=$IMAGESTREAM_NAME -p IMAGESTREAM_TAG=$ImageTag | oc create -f -
oc wait jobs/dev-applicantportal-dbmigrator --for condition=complete --timeout=120s -n $OC_TARGET_PROJECT