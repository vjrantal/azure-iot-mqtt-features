variable environment_id {
  type = string
  validation {
    condition     = length(var.environment_id) <= 4 && can(regex("[a-z0-9]", var.environment_id))
    error_message = "Must be alphanumberical lowercase and less than 4 chars."
  }
}
