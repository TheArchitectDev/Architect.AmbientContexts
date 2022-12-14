# Ambient Context

Provides the basis for implementing the Ambient Context pattern, as well as a Clock implementation based on it.

The Ambient Context pattern is an Inversion of Control (IoC) pattern that provides static access to a dependency while controlling the dependency from the outside.
The pattern optimizes accessiblity (through statics) at the cost of transparency, making it suitable for obvious, ubiquitous, rarely-changing dependencies.

A good example is System.Transactions.TransactionScope. Any code (such as the database connector) can access the static Transaction.Current, yet outer code in the current execution flow controls it, through TransactionScopes.

By inheriting from AmbientScope, a class can become an ambient scope much like TransactionScope. When code is wrapped in such a scope (with the help of a using statement), any code inside the scope can statically access that ambient scope.

The AmbientScope base class provides fine-grained control over scope nesting (by obscuring, combining, or throwing) and supports the registration of a ubiquitous default scope.

The implementation honors logical execution flows and is async-safe.

This package also includes the ClockScope, an Ambient Context implementation for accessing the clock.

## ClockScope

When using the clock, it is easy to lose the deterministic quality of unit tests, since the clock keeps advancing.
To keep tests deterministic, an IoC pattern can be employed to use a pinned clock instead of a the system clock.
However, the obvious pattern of Dependency Injection (DI) requires such a clock to be a _service_.
That would make it unsuitable for use from non-service types, such as entities, value objects, or resources.

ClockScope uses the Ambient Context pattern to solve this problem.

### Solution

Production code can obtain the time using `Clock.UtcNow` or `Clock.Now`.
Normally, no ambient ClockScope is present, and so the system clock (`DateTime.UtcNow`) is used as the source of time.

Outer code may construct a ClockScope to take control of the source of time, and dispose it again to revert to the situation of before it took control.
In advanced scenarios, such scopes could even be nested.

### Unit Tests

The following example shows a piece of production code using the clock, with an encapsulating unit test controlling the clock:

```cs
public class Order
{
	public DateTime CreationDateTime { get; }

	public Order()
	{
		this.CreationDateTime = Clock.UtcNow;
	}
}

public class OrderTests
{
	[Fact]
	public void Construct_Regularly_ShouldSetExpectedCreationDateTime()
	{
		using var clockScope = new ClockScope(DateTime.UnixEpoch);

		var result = new Order();

		Assert.Equal(DateTime.UnixEpoch, result.CreationDateTime);
		Assert.Equal(DateTimeKind.Utc, result.CreationDateTime.Kind);
	}
}
```

### Uniform Timestamps in Batches

When working on batches, it is sometimes desirable to assign the same timestamp to each item in a batch, even if the clock has actually advanced during the work.

The naive way to achieve this is to inject the timestamp into the operation that is performed on each element:

```cs
public class Order
{
	public void MarkAsShipped(DateTime timestamp, ShippingInfo shippingInfo, Username approver)
	{
		if (this.ShippingInfo is not null)
			throw new InvalidOperationException($"{this} was already shipped.");

		// "Right now", obviously, but we want each order in the batch to store the same timestamp
		this.ShippingDateTime = timestamp;

		this.ShippingInfo = shippingInfo;
		this.Approver = approver;
	}
}
```

However, that approach pollutes a method's parameters with an artificial timestamp. It often makes more sense for the method to simply use the current timestamp, since the operation is clearly taking place _now_.

ClockScope is useful in production code to get the best of both worlds:

```cs
public class Order
{
	public void MarkAsShipped(ShippingInfo shippingInfo, Username approver)
	{
		if (this.ShippingInfo is not null)
			throw new InvalidOperationException($"{this} was already shipped.");

		// This makes sense
		this.ShippingDateTime = Clock.UtcNow;

		this.ShippingInfo = shippingInfo;
		this.Approver = approver;
	}
}

public class ShipAllOrdersUseCase
{
	public void ShipAllOrders()
	{
		// Snip

		// Take the clock's current time, and pin it until the scope is disposed
		// Now the entire batch works with this timestamp
		using var clockScope = new ClockScope(Clock.UtcNow);

		foreach (var order in orders)
			order.MarkAsShipped(shippingInfo, approver);

		// Snip
	}
}
```

As a form of good practice, we still obtained the time passed to ClockScope from `Clock.UtcNow` (instead of from `DateTime.UtcNow`).
If there is ever any code that encapsulates the current code, and it wants to pin the clock from its own, higher level, it can do so, and we will automatically adhere to its decision.
