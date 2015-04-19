using System;
using System.Dynamic;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using SimpleInjector;

namespace Pop.Net
{
    public class SimpleInjectorDependencyResolver : IDependencyResolver
    {
        private readonly Container _container;
        private readonly ActorSystem _system;

        public SimpleInjectorDependencyResolver(Container container, ActorSystem system)
        {
            _container = container;
            _system = system;
            _system.AddDependencyResolver(this);
        }

        public Type GetType(string actorName)
        {
            var firstTry = Type.GetType(actorName);
            Func<Type> searchForType = () =>
            {
                return
                    AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(x => x.GetTypes())
                        .FirstOrDefault(t => t.Name.Equals(actorName));
            };
            return firstTry ?? searchForType();
        }

        public Func<ActorBase> CreateActorFactory(string actorName)
        {
            return () => (ActorBase) _container.GetInstance(GetType(actorName));
        }

        public Props Create<TActor>() where TActor : ActorBase
        {
            return _system.GetExtension<DIExt>().Props(typeof (TActor).Name);
        }

        public void Release(ActorBase actor)
        {            
        }        
    }

    public static class DependencyResolver
    {
        public static IDependencyResolver Instance { get; set; }
    }
}