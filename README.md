# Cloudflare DNS Updater
## Usage
Download the latest release and extract it. Use for example the task scheduler to run the CloudflareDNSUpdater.exe every few minutes with the first argument being your extracted folder containing the exe, dll, runtimeconfig and your cloudflare config.
Configure the cloudflare.config with a BearerToken and the zones you would like to update ([Generate Token](https://developers.cloudflare.com/fundamentals/api/get-started/create-token/)) and make sure, the token has access to all zones.

## Supported Platforms
The project is build with .NET 8, so every platform should be supported.

- Windows
- MacOS
- Linux