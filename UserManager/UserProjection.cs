using Marten.Events.Aggregation;

namespace UserManager;

public class UserProjection : SingleStreamProjection<User>
{
    public void Apply(Events.UserCreated created, User user)
    {
        user.Id = created.Id;
        user.Name = created.Name;
    }
}