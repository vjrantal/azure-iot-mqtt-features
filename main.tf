resource "azurerm_resource_group" "mqtt_iot" {
  name     = format("%s%s", var.environment_id, "_mqtt_iot")
  location = "West Europe"
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

  route {
    name           = "defaultroute"
    source         = "DeviceMessages"
    condition      = "true"
    endpoint_names = ["events"]
    enabled        = true
  }
}
