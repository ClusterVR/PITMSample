using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.Editor.Api.User
{
    [Serializable]
    public sealed class User
    {
        [SerializeField] string username;
        [SerializeField] string photoUrl;

        public string Username => username;
        public string PhotoUrl => photoUrl;

        public override string ToString()
        {
            return $"Username: {username}";
        }
    }
}
