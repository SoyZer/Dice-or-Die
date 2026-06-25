using UnityEngine;
public interface IGrabbable
{
    void Grab();
    void Release(UnityEngine.Vector3 throwVelocity);
    void SetHighlight(bool active, UnityEngine.Color color = default, float width = 3f);
}

public interface IDice
{
    void Roll(UnityEngine.Vector3 force, UnityEngine.Vector3 torque);
}

public interface IEffectable
{
    void ApplyModifier(IModifier modifier);
    void RemoveModifier(IModifier modifier);
}

public interface IModifier
{
    string NameKey { get; }
    void OnApply(PhysicalDice dice);
    void OnUpdate(PhysicalDice dice);
    void OnRemove(PhysicalDice dice);

    int ModifyReward(int currentReward);
    void OnRoll(PhysicalDice dice);
}