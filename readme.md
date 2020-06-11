# BigHack.Fumble.Api

## Deploying

Review the [release artifacts](https://github.com/totallymoney/BigHack.Fumble.Api).

Use `pick` for interactive deployments:

```bash
$ yarn pick
```

`pick` simply uses the underlying `deploy` command:

```bash
$ yarn deploy <version> <env> # e.g yarn deploy 0.1.2 prod
```

## Credit

* Project created by [fsharp-lambda-scaffold](https://github.com/mediaingenuity/fsharp-lambda-scaffold)
* Release tooling provided by [github-serverless-dotnet-artifacts](https://github.com/totallymoney/github-serverless-dotnet-artifacts)
