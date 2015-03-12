using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using System.Collections.Generic;

public class ReplayUser : User
{
    public ReplayUser(MsgData data_)
    {
        data = data_;
    }
    MsgData data;

    public int Id { get { return data.id; } }
    public bool IsItMe { get { return false; } }
    public bool IsPlayer { get { return data.playerID > 0; } }
    public bool IsSpectator { get { return false; } }
    public string Name { get { return data.name; } }
    public int PlayerId { get { return data.playerID; } }
    public int PrivilegeId { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public Sfs2X.Entities.Managers.IUserManager UserManager { get; set; }

    public bool ContainsVariable(string name) { return false; }
    public int GetPlayerId(Room room) { return data.playerID; }
    public UserVariable GetVariable(string varName) { return null; }
    public List<UserVariable> GetVariables() { return new List<UserVariable>(); }
    public bool IsAdmin() { return false; }
    public bool IsGuest() { return false; }
    public bool IsJoinedInRoom(Room room) { return true; }
    public bool IsModerator() { return false; }
    public bool IsPlayerInRoom(Room room) { return true; }
    public bool IsSpectatorInRoom(Room room) { return false; }
    public bool IsStandardUser() { return true; }
    public void RemovePlayerId(Room room) { }
    public void SetPlayerId(int id, Room room) { }
    public void SetVariable(UserVariable userVariable) { }
    public void SetVariables(ICollection<UserVariable> userVaribles) { }
}
