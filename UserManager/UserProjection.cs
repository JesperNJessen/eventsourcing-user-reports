using Marten.Events.Aggregation;

namespace UserManager;

public class UserProjection : SingleStreamProjection<User>
{
    public void Apply(Events.UserCreated created, User user)
    {
        user.Id = created.Id;
        user.Name = created.Name;
    }
    public void Apply(Events.UserUpdated created, User user)
    {
        user.Id = created.Id;
        user.Status = created.Status;
    }
}