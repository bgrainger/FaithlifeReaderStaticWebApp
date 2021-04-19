# Faithlife Reader

This is an [Azure Static Web App](https://docs.microsoft.com/en-us/azure/static-web-apps/overview) that displays
pages of items from your Faithlife news feed in chronological order.

It's currently deployed at https://red-dune-00b29c51e.azurestaticapps.net/

## Build Status

[![Azure Static Web Apps CI/CD](https://github.com/bgrainger/FaithlifeReaderStaticWebApp/actions/workflows/azure-static-web-apps.yml/badge.svg)](https://github.com/bgrainger/FaithlifeReaderStaticWebApp/actions/workflows/azure-static-web-apps.yml)

## Local Development

Install:

* [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)
* [Azure Static Web Apps CLI](https://github.com/Azure/static-web-apps-cli)

After cloning the repo, you will need to add the secrets the application needs:

```
func settings encrypt
func settings add ConsumerToken <consumer-token>
func settings add ConsumerSecret <consumer-secret>
func settings add CosmosConnectionString <connection-string>
func settings add SecretKey <Base64-encoded random 64-byte secret key>
```

Run `swa start` to start the Static Web App, and `func start --csharp` (in the `api` folder) to start the API.
