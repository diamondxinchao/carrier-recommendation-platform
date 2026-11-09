namespace Carrier.Domain;

public enum Equipment { DryVan, Refrigerated, Flatbed }

public sealed class CarrierProfile
{
    private CarrierProfile() { }

    public CarrierProfile(Guid id, string name, string dotNumber, Equipment equipment,
        int capacityLb, decimal ratePerMile, double onTimeRate, double safetyScore,
        int completedLoads, DateOnly availableFrom, DateOnly availableThrough)
    {
        if (id == Guid.Empty) throw new ArgumentException("An ID is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dotNumber);
        if (!Enum.IsDefined(equipment)) throw new ArgumentOutOfRangeException(nameof(equipment));
        if (capacityLb <= 0) throw new ArgumentOutOfRangeException(nameof(capacityLb));
        if (ratePerMile <= 0) throw new ArgumentOutOfRangeException(nameof(ratePerMile));
        if (!double.IsFinite(onTimeRate) || onTimeRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(onTimeRate));
        if (!double.IsFinite(safetyScore) || safetyScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(safetyScore));
        if (completedLoads < 0) throw new ArgumentOutOfRangeException(nameof(completedLoads));
        if (availableThrough < availableFrom) throw new ArgumentException("Invalid availability window.");

        Id = id; Name = name.Trim(); DotNumber = dotNumber.Trim(); Equipment = equipment;
        CapacityLb = capacityLb; RatePerMile = ratePerMile; OnTimeRate = onTimeRate;
        SafetyScore = safetyScore; CompletedLoads = completedLoads;
        AvailableFrom = availableFrom; AvailableThrough = availableThrough;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string DotNumber { get; private set; } = "";
    public Equipment Equipment { get; private set; }
    public int CapacityLb { get; private set; }
    public decimal RatePerMile { get; private set; }
    public double OnTimeRate { get; private set; }
    public double SafetyScore { get; private set; }
    public int CompletedLoads { get; private set; }
    public DateOnly AvailableFrom { get; private set; }
    public DateOnly AvailableThrough { get; private set; }
    public List<ServiceLane> Lanes { get; private set; } = [];

    public void AddLane(string origin, string destination)
    {
        var lane = new ServiceLane(Id, origin, destination);
        if (!Lanes.Any(x => x.Origin == lane.Origin && x.Destination == lane.Destination))
            Lanes.Add(lane);
    }
}

public sealed class ServiceLane
{
    private ServiceLane() { }
    public ServiceLane(Guid carrierId, string origin, string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        CarrierId = carrierId; Origin = origin.Trim().ToUpperInvariant();
        Destination = destination.Trim().ToUpperInvariant();
        if (Origin == Destination) throw new ArgumentException("Lane cities must differ.");
    }
    public Guid CarrierId { get; private set; }
    public string Origin { get; private set; } = "";
    public string Destination { get; private set; } = "";
}
