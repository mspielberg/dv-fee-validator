# Custom Fee Validator

Starting with DV r84, it is not possible to take jobs if the player has an outstanding debt over $5k. This makes it quite difficult to do jobs back to back, especially when it is not possible to service the current locomotive at the current station.

Initially, this mod excludes debts for consumable resources required for locomotives in the same trainset as the player's most-recently used locomotives. If the player has debts for cargo, car, or locomotive repairs, or owes a large amount in emissions taxes, these must still be paid off to accept a new job.

Other fee checking policies are possible to implement on request.

# Contributing

1. Fork this repository and clone the fork
1. Create a directory junction from local repo lib to your Derail Valley install: `mklink /J "PATH\TO\dv-fee-validator\lib" "PATH\TO\Derail Valley\DerailValley_Data\Managed"`
1. Make your changes, build, and TEST, TEST, TEST!
1. Open a pull request
