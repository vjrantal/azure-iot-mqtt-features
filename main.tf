resource "azurerm_resource_group" "mqtt_iot" {
  name     = format("%s_mqtt_iot", var.environment_id)
  location = "West Europe"
}

resource "azurerm_eventhub_namespace" "mqtt_iot" {
  name                = format("%s-custom-ns", var.environment_id)
  resource_group_name = azurerm_resource_group.mqtt_iot.name
  location            = azurerm_resource_group.mqtt_iot.location
  sku                 = "Basic"
}

resource "azurerm_eventhub" "mqtt_iot" {
  name                = format("%s_custom_eventhub", var.environment_id)
  resource_group_name = azurerm_resource_group.mqtt_iot.name
  namespace_name      = azurerm_eventhub_namespace.mqtt_iot.name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_eventhub_authorization_rule" "mqtt_iot" {
  resource_group_name = azurerm_resource_group.mqtt_iot.name
  namespace_name      = azurerm_eventhub_namespace.mqtt_iot.name
  eventhub_name       = azurerm_eventhub.mqtt_iot.name
  name                = "acctest"
  send                = true
}

resource "azurerm_iothub" "mqtt_iot" {
  name                = format("%s%s", var.environment_id, "-mqtt-iot-hub")
  resource_group_name = azurerm_resource_group.mqtt_iot.name
  location            = azurerm_resource_group.mqtt_iot.location

  # Define the Pricing Tier (SKU & Capacity)
  sku {
    name     = "S1"
    capacity = 1
  }

  endpoint {
    type              = "AzureIotHub.EventHub"
    connection_string = azurerm_eventhub_authorization_rule.mqtt_iot.primary_connection_string
    name              = "customevents"
  }

  route {
    name           = "customroute"
    source         = "DeviceMessages"
    condition      = "true"
    endpoint_names = ["customevents"]
    enabled        = true
  }

  route {
    name           = "defaultroute"
    source         = "DeviceMessages"
    condition      = "true"
    endpoint_names = ["events"]
    enabled        = true
  }
}
