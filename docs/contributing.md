# Contributing

This document is an introduction to contributing to the `azure-iot-mqtt-features` project.

## Workspace Setup & Building Project

This section describes how to get your developer workspace running for the first time so that you're ready to start making contributions. If you have already done this, check out [Running tests](#development-sdlc).

> Note: All steps are tested with the [.devcontainer/Dockerfile](../.devcontainer/Dockerfile) using bash

### 0. Get the code

* Clone the repository:
  
  ```bash
  git clone https://github.com/vjrantal/azure-iot-mqtt-features.git
  ```

### A) (Recommended) Docker devcontainer

To develop using docker:

> Note: It is highly recommended to use VSCode as described for rich intellisense, debugging, and access to standard go development tools.

If using VSCode's [Remote Development Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack):

* Open the project in VSCode
* When prompted by VSCode, select "Reopen in Container"

Without VSCode:

* Build the development environment: `docker build -f .devcontainer/Dockerfile -t dev .`
* Mount the root directory into the docker container, and drop into a bash shell in the container:
  
  ```bash
  docker run -it -v ${PWD}:/workspaces/terraform-provider-azuredevops dev
  ```

Continue with the guide to [run the provider locally](#4-run-provider-locally).

### B) Manually install dependencies

You will need the following dependencies installed in order to get started:

* [Dotnet Core](https://dotnet.microsoft.com/download/dotnet-core) version 3.1 +
* For provisioning infrastructure:
  * [Terraform](https://www.terraform.io/downloads.html) version 0.13.* +
  * [Az CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) 2.12.* +
  * [Az Iot CLI](https://github.com/Azure/azure-iot-cli-extension) 0.10.* +
* Windows:
  * [GNUMake for windows](http://gnuwin32.sourceforge.net/packages/make.htm) (For running [make commands](../makefile))
* An editor of your choice. We use [Visual Studio Code](https://code.visualstudio.com/Download) but any editor will do.

#### Setup your workspace

1. Provision Iot Hub with terraform:
     1. Set up [Authenticating terraform using the Az CLI](https://www.terraform.io/docs/providers/azurerm/guides/azure_cli.html)
     2. Execute terraform from the root folder:

        ```bash
        cd terraform
        terraform init
        terraform apply
        ```

2. Create a Iot Hub device

    ```bash
    DEVICE_ID=device0001 make create-device
    ```

3. Create `Properties/appsettings.json` from the [`appsettings.json.template`](../src/Properties/appsettings.json.template)
4. Populate secrets in `Properties/appsettings.json` with [make commands](../makefile) found above each secret
5. Generate a CA certificate as described in [`generate-CA-certificate.md`](generating-CA-certificate/generate-CA-certificate.md)
6. Generate a self-signed certificate by using [Portecle](https://sourceforge.net/projects/portecleinstall/) to export a private key .p12 file and then change the extension to .pfx file
7. Replace the [`CA-Certificate.pfx.template`](../src/Testing/Certificates/CA-Certificate.pfx.template) and [`SelfSigned-Certificate.pfx.template`](../src/Testing/Certificates/SelfSigned-Certificate.pfx.template) with the files generated previously.
