# Azure IoT MQTT features

![build](https://github.com/vjrantal/azure-iot-mqtt-features/workflows/build/badge.svg)

## Contributing

To get started with contributing, see [docs/contributing](docs/contributing.md)

## MQTT libraries

[Comparison of MQTT libraries](docs/MQTT-libraries.md)

## Test scenarios

### Client receives all C2D messages while disconnected with clean session set to false

* Subscribe to `devices/{device_id}/messages/devicebound/#`
* Send C2D and verify client receive
* Shutdown client
* Send another C2D
* Start client with clean session set to false and subscribe again
* Verify that client receives the message sent while being disconnected

### Client can send D2C messages with QoS 0

* Send D2C with QoS 0
* Verify that "most of them"* are received via IoT Hub

\* With QoS 0 there might be message loss but in normal good networking conditions, most messages are expected to be received

### Client can set Will message

* Client set Will message in the CONNECT packet and sets Will RETAIN
* Client use `devices/{device_id}/messages/events/$.ct=application%2Fjson&$.ce=utf-8` as the Will topic name
* Forcefully shutdown client (`ctrl+c`)
* Verify Will message received
* Verify the message has the `iothub-MessageType` property with a value of Will assigned to it
* Check whether `mqtt-retain` application property exists in the message (not documented what is expected)

### Client can send retained messages

* Client send D2C message with RETAIN flag set to 1
* Verify received
* Verify `mqtt-retain` application property in the message

### Client can authenticate with certificates

* Client uses a CA signed certificate
* Client uses a self-signed certificate
  
### Client messages can be routed (optional)

* Setup routing as described in <https://stackoverflow.com/questions/51160000/azure-iothub-devicemessage-and-route-filter>
* Send D2C to topic `devices/{device_id}/messages/events/$.ct=application%2Fjson&$.ce=utf-8`
* Verify routing is applied

## Outcome

* All test scenarios ran successfully
* The `x-opt-retain` application property actually appears as `mqtt-retain` so a [PR](https://github.com/MicrosoftDocs/azure-docs/pull/64738) was created and merged into [azure-docs](https://github.com/MicrosoftDocs/azure-docs)  repository to make a correction
  