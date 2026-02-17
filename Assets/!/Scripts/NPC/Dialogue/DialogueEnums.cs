/// <summary>
/// Types of dialogue nodes
/// </summary>
public enum NodeType
{
    NPC,      
    Player,   
    Event     
}

/// <summary>
/// Types of conditions that can be evaluated
/// </summary>
public enum ConditionType
{
    Bool,         
    IntEqual,     
    IntGreater,  
    IntLess,      
    FlippedBool   
}

/// <summary>
/// Types of effects that can be applied
/// </summary>
public enum EffectType
{
    SetBool,      
    SetInt,       
    AddInt,       
    StartQuest,   
    CompleteQuest 
}
