namespace ClusterVR.CreatorKit.Editor.Api.User
{
    public readonly struct UserInfo
    {
        public readonly User User;
        public readonly string VerifiedToken;

        public UserInfo(User user, string verifiedToken)
        {
            User = user;
            VerifiedToken = verifiedToken;
        }
    }
}
