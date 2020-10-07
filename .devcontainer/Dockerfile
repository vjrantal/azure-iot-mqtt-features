# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.140.1/containers/dotnetcore/.devcontainer/base.Dockerfile

# [Choice] .NET Core version: 3.1, 2.1
ARG VARIANT="3.1"
FROM mcr.microsoft.com/vscode/devcontainers/dotnetcore:0-${VARIANT}

# Avoid warnings by switching to noninteractive
ENV DEBIAN_FRONTEND=noninteractive

# [Option] Install Node.js
ARG NODE_VERSION="lts/*"
RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"

# Install additional OS packages
## Terraform and tflint versions
ARG TERRAFORM_VERSION=0.13.4
ARG TFLINT_VERSION=0.20.2

RUN apt-get update \
    && apt-get -y install --no-install-recommends apt-utils 2>&1 \
    # Verify git, process tools, lsb-release (common in install instructions for CLIs), unzip installed
    && apt-get -y install git procps lsb-release unzip \
    # Install the Azure CLI
    && curl -sL https://aka.ms/InstallAzureCLIDeb | bash \
    # Install AzDO CLI
    && az extension add --name azure-iot \
    # Install Terraform
    && mkdir -p /tmp/docker-downloads \
    && curl -sSL -o /tmp/docker-downloads/terraform.zip https://releases.hashicorp.com/terraform/${TERRAFORM_VERSION}/terraform_${TERRAFORM_VERSION}_linux_amd64.zip \
    && unzip /tmp/docker-downloads/terraform.zip \
    && mv terraform /usr/local/bin \
    # Install tflint
    && curl -sSL -o /tmp/docker-downloads/tflint.zip https://github.com/wata727/tflint/releases/download/v${TFLINT_VERSION}/tflint_linux_amd64.zip \
    && unzip /tmp/docker-downloads/tflint.zip \
    && mv tflint /usr/local/bin \
    # Clean up downloaded files
    && cd ~ \ 
    && rm -rf /tmp/docker-downloads \
    #
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# [Optional] Uncomment this line to install global node packages.
# RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && npm install -g <your-package-here>" 2>&1

# Switch back to dialog for any ad-hoc use of apt-get
ENV DEBIAN_FRONTEND=dialog