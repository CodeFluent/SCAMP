﻿namespace ScampApi.Infrastructure
{
    public interface ILinkHelper
    {
        string Group(int groupId);
        string GroupResource(int groupId, int resourceId);
        string GroupUser(int groupId, int userId);
    }
}