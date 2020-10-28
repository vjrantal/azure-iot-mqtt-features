DEVICE_ID?=device0001

create-device:
	az iot hub device-identity create --device-id $(DEVICE_ID) --hub-name $$(terraform output iothub_name)

list-devices: 
	az iot hub device-identity list --hub-name $$(terraform output iothub_name)

get-device-conn:
	az iot hub device-identity connection-string show --device-id $(DEVICE_ID) --hub-name $$(terraform output iothub_name)

get-iot-hub-conn:
	az iot hub connection-string show

get-event-hub-name:
	az iot hub show --query properties.eventHubEndpoints.events.path --name $$(terraform output iothub_name)

get-event-hub-endpoint:
	az iot hub show --query properties.eventHubEndpoints.events.endpoint --name $$(terraform output iothub_name)

get-iot-hub-sas-key:
	az iot hub policy show --name service --query primaryKey --hub-name $$(terraform output iothub_name)

get-custom-event-hub-name:
	terraform show -json | jq -r '.values.root_module.resources[] | select(.address=="azurerm_eventhub.mqtt_iot") | .values.name'

get-custom-event-hub-endpoint:
	namespace=$$(terraform show -json | jq -r '.values.root_module.resources[] | select(.address=="azurerm_eventhub_namespace.mqtt_iot") | .values.name') && \
	rg=$$(terraform show -json | jq -r '.values.root_module.resources[] | select(.address=="azurerm_resource_group.mqtt_iot") | .values.name') && \
	az eventhubs namespace authorization-rule keys list --resource-group $$rg --namespace-name $$namespace --name RootManageSharedAccessKey \
	| jq -r '.primaryConnectionString'

lint: md-lint md-spell-check md-link-check dotnet-lint

dotnet-lint:
	dotnet format --check

format:
	dotnet format

build:
	dotnet build

md-link-check:
	find . -name \*.md -exec markdown-link-check {} \;

md-spell-check:
	mdspell --en-us --ignore-acronyms --ignore-numbers  '**/*.md' --report

md-spell-check-interactive:
	mdspell --en-us --ignore-acronyms --ignore-numbers  '**/*.md'

md-lint:
	markdownlint '**/*.md'