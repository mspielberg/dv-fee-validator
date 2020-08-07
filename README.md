# Custom Fee Validator

Starting with DV r84, it is not possible to take jobs if the player has an outstanding debt over $5k. This makes it quite difficult to do jobs back to back, especially when it is not possible to service the current locomotive at the current station.

## Fee Validation Schemes

This mod offers a few fee validation schemes to choose from.

### Ignore consumables fees from all locomotives coupled to the last entered locomotive

This fee validation scheme excludes debts for consumable resources (fuel, sand, etc.) used by locomotives that are part of the same trainset as the player's most-recently used locomotive. If the player has debts for damaged cargo or train cars, locomotive repairs, or owes a large amount in emissions taxes, these must still be paid off to accept a new job.

### Ignore all fees from all locomotives that have not yet despawned

This fee validation scheme excludes all debts (consumables, repairs, etc.) for all locomotives that still exist in the world. If the player has debts for damaged cargo or train cars, these must still be paid off to accept a new job.

Other fee checking policies are possible to implement. Suggestions and pull requests are welcome.

# Contributing

1. Fork this repository and clone the fork
1. Create a symbolic link named dv in your local repo targeting your Derail Valley installation, for example: `mklink /D "C:\Users\whoami\source\repos\dv-fee-validator\dv" "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley"`
    * Note: you must run Command Prompt as Administrator in order to use `mklink`
1. Make your changes, build, and TEST, TEST, TEST!
1. Open a pull request
