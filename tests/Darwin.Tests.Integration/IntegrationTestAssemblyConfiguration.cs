using Xunit;

// Integration suites execute destructive database reset/seed operations.
// Disabling assembly-level parallelization prevents concurrent resets from
// different classes from racing against each other and introducing flakiness.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
