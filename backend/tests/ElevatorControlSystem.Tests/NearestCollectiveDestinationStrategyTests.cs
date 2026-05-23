using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Tests;

public sealed class NearestCollectiveDestinationStrategyTests
{
    private readonly NearestCollectiveDestinationStrategy _strategy = new();

    private static SimulationSession CreateSession(int floorCount = 10)
    {
        var session = new SimulationSession
        {
            FloorCount = floorCount,
            ElevatorState = new ElevatorState
            {
                CurrentFloor = 1,
                CurrentPosition = 1m,
                MovementState = ElevatorMovementState.IdleClosed,
                Direction = Direction.None
            }
        };

        for (var floor = 1; floor <= floorCount; floor++)
        {
            session.FloorCalls.Add(new FloorCall
            {
                FloorNumber = floor,
                IsPressed = false
            });

            session.CabinRequests.Add(new CabinRequest
            {
                FloorNumber = floor,
                IsPressed = false
            });
        }

        return session;
    }

    [Fact]
    public void HasAnyRequests_WhenNoButtonsPressed_ReturnsFalse()
    {
        var session = CreateSession();

        var result = _strategy.HasAnyRequests(session);

        Assert.False(result);
    }

    [Fact]
    public void HasAnyRequests_WhenFloorCallPressed_ReturnsTrue()
    {
        var session = CreateSession();
        session.FloorCalls.Single(x => x.FloorNumber == 4).IsPressed = true;

        var result = _strategy.HasAnyRequests(session);

        Assert.True(result);
    }

    [Fact]
    public void SelectDirectionFromIdle_WhenNearestRequestAbove_ReturnsUp()
    {
        var session = CreateSession();
        session.CabinRequests.Single(x => x.FloorNumber == 7).IsPressed = true;

        var result = _strategy.SelectDirectionFromIdle(session, currentFloor: 3);

        Assert.Equal(Direction.Up, result);
    }

    [Fact]
    public void SelectDirectionFromIdle_WhenNearestRequestBelow_ReturnsDown()
    {
        var session = CreateSession();
        session.FloorCalls.Single(x => x.FloorNumber == 2).IsPressed = true;

        var result = _strategy.SelectDirectionFromIdle(session, currentFloor: 8);

        Assert.Equal(Direction.Down, result);
    }

    [Fact]
    public void SelectDirectionFromIdle_WhenCurrentFloorHasRequest_IgnoresCurrentFloor()
    {
        var session = CreateSession();
        session.FloorCalls.Single(x => x.FloorNumber == 5).IsPressed = true;
        session.CabinRequests.Single(x => x.FloorNumber == 8).IsPressed = true;

        var result = _strategy.SelectDirectionFromIdle(session, currentFloor: 5);

        Assert.Equal(Direction.Up, result);
    }

    [Fact]
    public void HasRequestsInDirection_WhenRequestExistsOnlyAbove_ReturnsTrueOnlyForUp()
    {
        var session = CreateSession();
        session.FloorCalls.Single(x => x.FloorNumber == 9).IsPressed = true;

        var hasUpRequests = _strategy.HasRequestsInDirection(
            session,
            currentFloor: 4,
            direction: Direction.Up);

        var hasDownRequests = _strategy.HasRequestsInDirection(
            session,
            currentFloor: 4,
            direction: Direction.Down);

        Assert.True(hasUpRequests);
        Assert.False(hasDownRequests);
    }

    [Fact]
    public void ShouldStopAtFloor_WhenFloorCallPressed_ReturnsTrue()
    {
        var session = CreateSession();
        session.FloorCalls.Single(x => x.FloorNumber == 6).IsPressed = true;

        var result = _strategy.ShouldStopAtFloor(session, floorNumber: 6);

        Assert.True(result);
    }

    [Fact]
    public void ShouldStopAtFloor_WhenCabinRequestPressed_ReturnsTrue()
    {
        var session = CreateSession();
        session.CabinRequests.Single(x => x.FloorNumber == 3).IsPressed = true;

        var result = _strategy.ShouldStopAtFloor(session, floorNumber: 3);

        Assert.True(result);
    }

    [Fact]
    public void ShouldStopAtFloor_WhenPassengerTargetsFloor_ReturnsTrue()
    {
        var session = CreateSession();

        session.Passengers.Add(new Passenger
        {
            WeightKg = 80m,
            SourceFloor = 1,
            TargetFloor = 7,
            CurrentFloor = 6,
            Status = PassengerStatus.Riding
        });

        var result = _strategy.ShouldStopAtFloor(session, floorNumber: 7);

        Assert.True(result);
    }

    [Fact]
    public void ShouldStopAtFloor_WhenNoStopReason_ReturnsFalse()
    {
        var session = CreateSession();

        var result = _strategy.ShouldStopAtFloor(session, floorNumber: 4);

        Assert.False(result);
    }
}
