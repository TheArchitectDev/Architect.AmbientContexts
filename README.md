# Ambient Context

Provides the basis for implementing the Ambient Context pattern, as well as a Clock implementation based on it.

The Ambient Context pattern is an Inversion of Control (IoC) pattern that provides static access to a dependency while controlling the dependency from the outside.
The pattern optimizes accessiblity (through statics) at the cost of transparency, making it suitable for obvious, ubiquitous, rarely-changing dependencies.

A good example is System.Transactions.TransactionScope. Any code (such as the database connector) can access the static Transaction.Current, yet outer code in the current execution flow controls it, through TransactionScopes.

By inheriting from AmbientScope, a class can become an ambient scope much like TransactionScope. When code is wrapped in such a scope (with the help of a using statement), any code inside the scope can statically access that ambient scope.

The AmbientScope base class provides fine-grained control over scope nesting (by obscuring, combining, or throwing) and supports the registration of a ubiquitous default scope.

The implementation honors logical execution flows and is async-safe.

This package also includes the ClockScope, an Ambient Context implementation for accessing the clock.
