

public interface IMarblePowerState
{
    MarblePowerType Type { get; }
    abstract int TurnsLeft { get; }
    abstract void OnEnter();
    abstract void OnExit();


    abstract void OnTurnEnded();


    abstract float GetLaunchMultiplier(float baseMultiplier);
    abstract void OnTurnBecameCurrent(bool isCurrentTurn);
}
