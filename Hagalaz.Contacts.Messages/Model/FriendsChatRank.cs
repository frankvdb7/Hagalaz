namespace Hagalaz.Contacts.Messages.Model
{
    public enum FriendsChatRank : sbyte
    {
        Banned = -3,
        NoOne = -2,
        Regular = -1,
        Friend = 0,
        Recruit = 1,
        Corporal = 2,
        Sergeant = 3,
        Lieutenant = 4,
        Captain = 5,
        General = 6,
        Owner = 7,
        Admin = 127
    }
}