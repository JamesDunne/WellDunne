using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Patterns
{
    public interface IResource<T> where T : class, IDisposable
    {
        /// <summary>
        /// Use the provided resource and return a value.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="act"></param>
        /// <returns></returns>
        U Use<U>(Func<T, U> act);

        /// <summary>
        /// Use the provided resource.
        /// </summary>
        /// <param name="act"></param>
        void Act(Action<T> act);
    }

    /// <summary>
    /// Represents a resource that is created and disposed of per each use.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct OnDemandResource<T> : IResource<T> where T : class, IDisposable
    {
        public Func<T> CreateResource { get; private set; }

        public OnDemandResource(Func<T> createResource)
            : this()
        {
            if (createResource == null) throw new ArgumentNullException("createResource");
            this.CreateResource = createResource;
        }

        /// <summary>
        /// Creates a new instance of the resource, uses it, and dispose of it.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="act"></param>
        /// <returns></returns>
        public U Use<U>(Func<T, U> act)
        {
            using (T resource = CreateResource())
                return act(resource);
        }

        /// <summary>
        /// Creates a new instance of the resource, uses it, and dispose of it.
        /// </summary>
        /// <param name="act"></param>
        public void Act(Action<T> act)
        {
            using (T resource = CreateResource())
                act(resource);
        }
    }

    /// <summary>
    /// Represents an existing resource that the user must not dispose of.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct UseExistingResource<T> : IResource<T> where T : class, IDisposable
    {
        public T Resource { get; private set; }

        public UseExistingResource(T resource)
            : this()
        {
            if (resource == null) throw new ArgumentNullException("resource");
            this.Resource = resource;
        }

        /// <summary>
        /// Reuses an existing resource. It is an error to dispose of it.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="act"></param>
        /// <returns></returns>
        public U Use<U>(Func<T, U> act)
        {
            return act(Resource);
        }

        /// <summary>
        /// Reuses an existing resource. It is an error to dispose of it.
        /// </summary>
        /// <param name="act"></param>
        public void Act(Action<T> act)
        {
            act(Resource);
        }
    }

    public static class Resource
    {
        /// <summary>
        /// Create a disposable resource as needed on demand.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createResource"></param>
        /// <returns></returns>
        public static OnDemandResource<T> MakeOnDemand<T>(Func<T> createResource) where T : class, IDisposable
        {
            return new OnDemandResource<T>(createResource);
        }

        /// <summary>
        /// Wrap an existing disposable resource that the user must not dispose of.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static UseExistingResource<T> UseExisting<T>(this T resource) where T : class, IDisposable
        {
            return new UseExistingResource<T>(resource);
        }
    }
}
