namespace ElevatorControlSystem.Domain;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IDestinationSelectionStrategy
{
    bool HasAnyRequests(SimulationSession session);

    bool HasRequestsInDirection(SimulationSession session, int currentFloor, Direction direction);

    Direction SelectDirectionFromIdle(SimulationSession session, int currentFloor);

    bool ShouldStopAtFloor(SimulationSession session, int floorNumber);
}

public sealed class NearestCollectiveDestinationStrategy : IDestinationSelectionStrategy
{
    public bool HasAnyRequests(SimulationSession session)
    {
        return GetActiveFloors(session).Any();
    }

    public bool HasRequestsInDirection(
        SimulationSession session,
        int currentFloor,
        Direction direction)
    {
        return direction switch
        {
            Direction.Up => GetActiveFloors(session).Any(x => x > currentFloor),
            Direction.Down => GetActiveFloors(session).Any(x => x < currentFloor),
            _ => false
        };
    }

    public Direction SelectDirectionFromIdle(SimulationSession session, int currentFloor)
    {
        var activeFloors = GetActiveFloors(session).ToList();

        // Текущий этаж здесь намеренно исключается из выбора.
        // Если этого не сделать, то при наличии вызова на текущем этаже
        // стратегия будет постоянно возвращать отсутствие направления,
        // а реальная логика может начать "переслуживать" тот же этаж.
        var nearestDifferentFloor = activeFloors
            .Where(x => x != currentFloor)
            .OrderBy(x => Math.Abs(x - currentFloor))
            .ThenBy(x => x)
            .FirstOrDefault();

        if (nearestDifferentFloor == 0)
        {
            return Direction.None;
        }

        if (nearestDifferentFloor > currentFloor)
        {
            return Direction.Up;
        }

        if (nearestDifferentFloor < currentFloor)
        {
            return Direction.Down;
        }

        return Direction.None;
    }

    public bool ShouldStopAtFloor(SimulationSession session, int floorNumber)
    {
        var hasFloorCall = session.FloorCalls.Any(
            x => x.FloorNumber == floorNumber && x.IsPressed);

        var hasCabinRequest = session.CabinRequests.Any(
            x => x.FloorNumber == floorNumber && x.IsPressed);

        var hasPassengerForExit = session.Passengers.Any(
            x => x.Status == PassengerStatus.Riding && x.TargetFloor == floorNumber);

        return hasFloorCall || hasCabinRequest || hasPassengerForExit;
    }

    private static IEnumerable<int> GetActiveFloors(SimulationSession session)
    {
        var floorCalls = session.FloorCalls
            .Where(x => x.IsPressed)
            .Select(x => x.FloorNumber);

        var cabinRequests = session.CabinRequests
            .Where(x => x.IsPressed)
            .Select(x => x.FloorNumber);

        return floorCalls
            .Concat(cabinRequests)
            .Distinct();
    }
}
