# Managing test CA certificates for connecting a device to the IoT Hub

## WARNING

Certificates created by these scripts **MUST NOT** be used for production.  They contain hard-coded passwords ("1234"), expire after 30 days, and most importantly are provided for demonstration purposes to help you quickly understand CA Certificates.  When using CA Certificates in production, you'll need to use your own security best practices for certification creation and lifetime management.

## Introduction

The tools in this file can be used to setup CA Certificates (along with proof of possession).

This file contains a Bash script to help create **test** certificates for Azure IoT Hub's CA Certificate / proof-of-possession.
A more detailed document showing UI screen shots for CA Certificates and proof of possession flow is available from [the official documentation].

## USE

### Step 1 - Initial Setup

You'll need to do some initial setup prior to running these script.

* Start Bash.
* `cd` to the directory you want to run in.  All files will be created as children of this directory.
* `cp *.cnf` and `cp *.sh` from the directory this .MD file is located into your working directory.
* `chmod 700 certGen.sh`

### Step 2 - Create the certificate chain

First you need to create a CA and an intermediate certificate signer that chains back to the CA.

* Run `./certGen.sh create_root_and_intermediate`

Next, go to Azure IoT Hub and navigate to Certificates.  Add a new certificate, providing the root CA file when prompted.  (`./certs/azure-iot-test-only.root.ca.cert.pem`)

### Step 3 - Proof of Possession

Now that you've registered your root CA with Azure IoT Hub, you'll need to prove that you actually own it.

Select the new certificate that you've created and navigate to and select  "Generate Verification Code".  This will give you a verification string you will need to place as the subject name of a certificate that you need to sign.  For our example, assume IoT Hub verification code was "106A5SD242AF512B3498BD6098C4941E66R34H268DDB3288", the certificate subject name should be that code. See below example Bash script

* Run `./certGen.sh create_verification_certificate 106A5SD242AF512B3498BD6098C4941E66R34H268DDB3288`

The scripts will output the name of the file containing `"CN=106A5SD242AF512B3498BD6098C4941E66R34H268DDB3288"` to the console.  Upload this file to IoT Hub (in the same UX that had the "Generate Verification Code") and select "Verify".

### Step 4 - Create a new device

On Azure IoT Hub, navigate to the IoT Devices section, or launch Azure IoT Explorer.  Add a new device (e.g. `mydevice`), and for its authentication type chose "X.509 CA Signed".  Devices can authenticate to IoT Hub using a certificate that is signed by the Root CA from Step 2.

* Run `./certGen.sh create_device_certificate mydevice` to create the new device certificate.  
  This will create the files ./certs/new-device.* that contain the public key and PFX and ./private/new-device.key.pem that contains the device's private key.  

* `cd ./certs && cat new-device.cert.pem azure-iot-test-only.intermediate.cert.pem azure-iot-test-only.root.ca.cert.pem > new-device-full-chain.cert.pem` to get the public key.

[the official documentation]: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-x509-get-started
