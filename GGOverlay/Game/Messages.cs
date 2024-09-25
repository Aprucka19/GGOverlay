﻿// Messages.cs

using GGOverlay.Game;

public class Message
{
    public string MessageType { get; set; }
}

public class PlayerUpdateMessage : Message
{
    public PlayerInfo Player { get; set; }

    public PlayerUpdateMessage()
    {
        MessageType = "PLAYERUPDATE";
    }
}

public class RuleUpdateMessage : Message
{
    public List<Rule> Rules { get; set; }

    public RuleUpdateMessage()
    {
        MessageType = "RULEUPDATE";
    }
}

public class PlayerListUpdateMessage : Message
{
    public List<PlayerInfo> Players { get; set; }

    public PlayerListUpdateMessage()
    {
        MessageType = "PLAYERLISTUPDATE";
    }
}

public class TriggerIndividualRuleMessage : Message
{
    public Rule Rule { get; set; }
    public PlayerInfo Player { get; set; }

    public TriggerIndividualRuleMessage()
    {
        MessageType = "TRIGGERINDIVIDUALRULE";
    }
}

public class TriggerGroupRuleMessage : Message
{
    public Rule Rule { get; set; }

    public TriggerGroupRuleMessage()
    {
        MessageType = "TRIGGERGROUPRULE";
    }
}