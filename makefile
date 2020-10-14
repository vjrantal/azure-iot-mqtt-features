DEVICE_ID=device0001

create-device:
	az iot hub device-identity create --device-id $(DEVICE_ID) --hub-name $$(terraform output iothub_name)

list-devices: 
	az iot hub device-identity list --hub-name $$(terraform output iothub_name)

get-device-conn:
	az iot hub device-identity connection-string show --device-id $(DEVICE_ID) --hub-name $$(terraform output iothub_name)

get-iot-hub-conn:
	az iot hub connection-string show