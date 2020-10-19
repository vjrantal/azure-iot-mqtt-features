# Contributing

This document is an introduction to contributing to the `azure-iot-mqtt-features` project.

## Workspace Setup & Building Project

This section describes how to get your developer workspace running for the first time so that you're ready to start making contributions. If you have already done this, check out [Running tests](#development-sdlc).

> Note: All steps are tested with the [.devcontainer/Dockerfile](../.devcontainer/Dockerfile) using bash

### 0. Get the code

* Clone the repo:
  
  ```bash
  git clone https://github.com/vjrantal/azure-iot-mqtt-features.git
  ```

### 1.A (Recommended) Docker devcontainer

To develop using docker:

> Note: It is highly recommended to use VSCode as described for rich intellisense, debugging, and access to standard go development tools.

If using VSCode's [Remote Development Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack):

* Open the project in VSCode
* When prompted by VSCode, select "Reopen in Container"

Without VSCode:

* Build the development environment: `docker build -f .devcontainer/Dockerfile -t dev .`
* From bash or powershell, mount the root directory into the docker container, and drop into a bash shell in the container:
  
  ```bash
  docker run -it -v ${PWD}:/workspaces/terraform-provider-azuredevops dev
  ```

Continue with the guide to [run the provider locally](#4-run-provider-locally).

### 1.B Manually install dependencies

You will need the following dependencies installed in order to get started:

* [Dotnet Core](https://dotnet.microsoft.com/download/dotnet-core) version 3.1 +
* For provisioning infrastructure:
  * [Terraform](https://www.terraform.io/downloads.html) version 0.13.x +
  * [Az CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) 2.12.x +
  * [Az Iot CLI](https://github.com/Azure/azure-iot-cli-extension) 0.10.x +
* Windows:
  * [GNUMake for windows](http://gnuwin32.sourceforge.net/packages/make.htm) (For running [makefile](../makefile) commands)
* An editor of your choice. We use [Visual Studio Code](https://code.visualstudio.com/Download) but any editor will do.

#### Setup your workspace

1. Provision IotHub with terraform:
     1. Set up [Authenticating terraform using the Az CLI](https://www.terraform.io/docs/providers/azurerm/guides/azure_cli.html)
     2. Execute terraform from the root folder:

        ```bash
        terraform init
        terraform apply
        ```

2. Create a IotHub device

    ```bash
    DEVICE_ID=device0001 make create-device
    ```

3. Create `Properties/appsettings.json` from the [appsettings.json.template](../Properties/appsettings.json.template)
4. Populate secrets in `Properties/appsettings.json`

    ```bash
    DEVICE_ID=device0001 make get-device-conn
    make get-iot-hub-conn
    ```
