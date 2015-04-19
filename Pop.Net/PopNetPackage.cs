using System.Linq;
using System.Reflection;
using System.Threading;
using Akka.Actor;
using SimpleInjector;
using SimpleInjector.Packaging;

namespace Pop.Net
{
    public class PopNetPackage : IPackage
    {
        public void RegisterServices(Container container)
        {
            container.Register(() => new CancellationTokenSource(), Lifestyle.Singleton);
            container.Register<IMailDrop, MailDrop>();
            foreach (
                var actorType in
                    Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(ActorBase).IsAssignableFrom(t)))
            {
                container.Register(actorType);
            }           
        }
    }
}