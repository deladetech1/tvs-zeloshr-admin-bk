#!/bin/bash
set -e

ACR_NAME="${ACR_NAME:-trovesuiteprodacr}"
IMAGE_NAME="${IMAGE_NAME:-zeloshr}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
DOCKERFILE_PATH="${DOCKERFILE_PATH:-./app/Dockerfile}"
BUILD_CONTEXT="${BUILD_CONTEXT:-.}"
PLATFORMS="linux/amd64,linux/arm64"

FULL_IMAGE_NAME="${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"

echo "Building ${FULL_IMAGE_NAME}..."
docker buildx build \
  --platform "${PLATFORMS}" \
  -f "${DOCKERFILE_PATH}" \
  -t "${FULL_IMAGE_NAME}" \
  "${BUILD_CONTEXT}" \
  --push

echo "Done: ${FULL_IMAGE_NAME}"
