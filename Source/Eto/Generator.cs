using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Eto
{
	/// <summary>
	/// Arguments for when a widget is created
	/// </summary>
	/// <copyright>(c) 2012 by Curtis Wensley</copyright>
	/// <license type="BSD-3">See LICENSE for full terms</license>
	public class WidgetCreatedArgs : EventArgs
	{
		/// <summary>
		/// Gets the instance of the widget that was created
		/// </summary>
		public object Instance { get; private set; }

		/// <summary>
		/// Initializes a new instance of the WidgetCreatedArgs class
		/// </summary>
		/// <param name="instance">Instance of the widget that was created</param>
		public WidgetCreatedArgs(object instance)
		{
			this.Instance = instance;
		}
	}

	/// <summary>
	/// Extensions for the <see cref="Generator"/> class
	/// </summary>
	/// <copyright>(c) 2012 by Curtis Wensley</copyright>
	/// <license type="BSD-3">See LICENSE for full terms</license>
	public static class GeneratorExtensions
	{
		/// <summary>
		/// Creates a new instance of the handler of the specified type of <typeparamref name="T"/>
		/// </summary>
		/// <remarks>
		/// This extension should be used when creating instances of a fixed type.
		/// This is a helper so that you can use a null generator variable to create instances with the current
		/// generator without having to do the extra check
		/// </remarks>
		/// <typeparam name="T">Type of handler to create</typeparam>
		/// <param name="generator">Generator to create the instance, or null to use the current generator</param>
		/// <returns>A new instance of a handler</returns>
		public static T Create<T>(this Generator generator)
		{
			return (T)(generator ?? Generator.Current).Create(typeof(T));
		}

		/// <summary>
		/// Creates a shared singleton instance of the specified type of <typeparamref name="T"/>
		/// </summary>
		/// <remarks>
		/// This extension should be used when creating shared instances of a fixed type.
		/// This is a helper so that you can use a null generator variable to create instances with the current
		/// generator without having to do the extra check
		/// </remarks>
		/// <param name="generator">Generator to create or get the shared instance, or null to use the current generator</param>
		/// <typeparam name="T">The type of handler to get a shared instance for</typeparam>
		/// <returns>The shared instance of a handler of the given type, or a new instance if not already created</returns>
		public static T CreateShared<T>(this Generator generator)
		{
			return (T)(generator ?? Generator.Current).CreateShared(typeof(T));
		}

		/// <summary>
		/// Finds the delegate to create instances of the specified type
		/// </summary>
		/// <typeparam name="T">Type of the handler interface (usually derived from <see cref="IWidget"/> or another type)</typeparam>
		/// <returns>The delegate to use to create instances of the specified type</returns>
		public static Func<T> Find<T>(this Generator generator)
			where T: class
		{
			return (Func<T>)(generator ?? Generator.Current).Find(typeof(T));
		}

		public static Dictionary<TKey, TValue> Cache<TKey, TValue>(this Generator generator, object cacheKey)
		{
			return (generator ?? Generator.Current).GetSharedProperty <Dictionary<TKey, TValue>>(cacheKey, () => new Dictionary<TKey, TValue>());
		}
	}

	/// <summary>
	/// Base generator class for each platform
	/// </summary>
	/// <remarks>
	/// The generator takes care of creating the platform-specific implementations of each
	/// control. Typically, the types are automatically found from the platform assembly, however
	/// you can also create your own platform-specific controls by adding the types manually via
	/// <see cref="Generator.Add"/>
	/// 
	/// The types are found by the interface of the control.  For example the <see cref="Forms.Label"/> control
	/// uses the <see cref="Forms.ILabel"/> interface for its platform implementation.  The generator
	/// will automatically scan an assembly for a class that directly implements this interface
	/// for its platform implementation (if it hasn't been added manually).
	/// </remarks>
	/// <copyright>(c) 2012 by Curtis Wensley</copyright>
	/// <license type="BSD-3">See LICENSE for full terms</license>
	public abstract class Generator
	{
		readonly Dictionary<Type, Func<object>> instantiatorMap = new Dictionary<Type, Func<object>>();
		readonly Dictionary<Type, object> sharedInstances = new Dictionary<Type, object>();
		readonly Dictionary<object, object> properties = new Dictionary<object, object>();
		static Generator globalInstance;
		static ThreadLocal<Generator> instance = new ThreadLocal<Generator>(() => globalInstance, false);

		internal T GetSharedProperty<T>(object key, Func<T> instantiator)
		{
			object value;
			lock (properties)
			{
				if (!properties.TryGetValue(key, out value))
				{
					value = instantiator();
					properties[key] = value;
				}
			}
			return (T)value;
		}

		#region Events

		/// <summary>
		/// Event to handle when widgets are created by this generator
		/// </summary>
		public event EventHandler<WidgetCreatedArgs> WidgetCreated;

		/// <summary>
		/// Handles the <see cref="WidgetCreated"/> event
		/// </summary>
		/// <param name="e">Arguments for the event</param>
		protected virtual void OnWidgetCreated(WidgetCreatedArgs e)
		{
			if (WidgetCreated != null)
				WidgetCreated(this, e);
		}

		#endregion

		/// <summary>
		/// Gets the ID of this generator
		/// </summary>
		/// <remarks>
		/// The generator ID can be used to determine which generator is currently in use.  The generator
		/// does not necessarily correspond to the OS that it is running on, as for example the GTK platform
		/// can run on OS X and Windows.
		/// </remarks>
		public abstract string ID { get; }

		/// <summary>
		/// Gets a value indicating whether this generator is a mac based platform (MonoMac/XamMac)
		/// </summary>
		/// <value><c>true</c> if this generator is mac; otherwise, <c>false</c>.</value>
		public virtual bool IsMac { get { return false; } }

		/// <summary>
		/// Gets a value indicating whether this generator is based on Windows Forms
		/// </summary>
		/// <value><c>true</c> if this generator is window forms; otherwise, <c>false</c>.</value>
		public virtual bool IsWinForms { get { return false; } }

		/// <summary>
		/// Gets a value indicating whether this generator is based on WPF
		/// </summary>
		/// <value><c>true</c> if this generator is wpf; otherwise, <c>false</c>.</value>
		public virtual bool IsWpf { get { return false; } }

		/// <summary>
		/// Gets a value indicating whether this generator is based on GTK# (2 or 3)
		/// </summary>
		/// <value><c>true</c> if this generator is gtk; otherwise, <c>false</c>.</value>
		public virtual bool IsGtk { get { return false; } }

		/// <summary>
		/// Gets a value indicating whether this generator is based on Xamarin.iOS
		/// </summary>
		/// <value><c>true</c> if this generator is ios; otherwise, <c>false</c>.</value>
		public virtual bool IsIos { get { return false; } }

		/// <summary>
		/// Gets a value indicating whether this generator is based on Xamarin.Android.
		/// </summary>
		/// <value><c>true</c> if this generator is android; otherwise, <c>false</c>.</value>
		public virtual bool IsAndroid { get { return false; } }

		public virtual bool IsDesktop { get { return false; } }

		public virtual bool IsMobile { get { return false; } }

		/// <summary>
		/// Initializes a new instance of the Generator class
		/// </summary>
		protected Generator()
		{
		}

		/// <summary>
		/// Gets a value indicating that the specified type is supported by this generator
		/// </summary>
		/// <typeparam name="T">type to test for</typeparam>
		/// <returns>true if the specified type is supported, false otherwise</returns>
		public virtual bool Supports<T>()
			where T: class
		{
			return this.Find<T>() != null;
		}

		/// <summary>
		/// Gets the generator for the current thread
		/// </summary>
		/// <remarks>
		/// Typically you'd have only one platform generator active at a time, and this holds an instance
		/// to that value.  The current generator is set automatically by the <see cref="Forms.Application"/> class
		/// when it is initially created.
		/// 
		/// This will be used when creating controls, unless explicitly passed through the constructor of the
		/// control. This allows you to use multiple generators at one time.
		/// </remarks>
		public static Generator Current
		{
			get
			{
				//if (current == null)
				//	throw new EtoException("Generator has not been initialized");
				return instance.Value;
			}
		}

		/// <summary>
		/// Returns true if the current generator has been set.
		/// </summary>
		public static bool HasCurrent { get { return globalInstance != null; } }

#if !PCL
		/// <summary>
		/// Returns the current generator, or detects the generator to use if no current generator is set.
		/// </summary>
		/// <remarks>
		/// This detects the platform to use based on the generator assemblies available and the current OS.
		/// 
		/// For windows, it will prefer WPF to Windows Forms.
		/// Mac OS X will prefer the Mac platform.
		/// Other unix-based platforms will prefer GTK.
		/// </remarks>
		public static Generator Detect
		{
			get
			{
				if (current != null)
					return current;

				Generator detected = null;
			
				if (EtoEnvironment.Platform.IsMac) {
					detected = Generator.GetGenerator (Generators.XamMacAssembly, true);
					if (detected == null)
						detected = Generator.GetGenerator (Generators.MacAssembly, true);
				}
				else if (EtoEnvironment.Platform.IsWindows) {
					detected = Generator.GetGenerator (Generators.WpfAssembly, true);
					if (detected == null)
						detected = Generator.GetGenerator (Generators.WinAssembly, true);
				}

				if (detected == null && EtoEnvironment.Platform.IsUnix)
					detected = Generator.GetGenerator (Generators.GtkAssembly, true);
				
				if (detected == null)
					throw new EtoException("Could not detect platform. Are you missing a platform assembly?");
					
				Initialize(detected);
				return current;
			}
		}
#endif

		/// <summary>
		/// Can be used by apps that switch between generators.
		/// 
		/// Set this property at the start of a block of code.
		/// All objects created after that point are verified to
		/// use this generator.
		/// 
		/// If null, no validation is performed.
		/// </summary>
		public static Generator ValidateGenerator { get; set; }

		/// <summary>
		/// Called by handlers to make sure they use the generator
		/// specified by ValidateGenerator
		/// </summary>
		/// <param name="generator"></param>
		[Conditional("DEBUG")]
		public static void Validate(Generator generator)
		{
			if (ValidateGenerator != null && !object.ReferenceEquals(generator, ValidateGenerator))
			{
				throw new EtoException(string.Format(CultureInfo.InvariantCulture, "Expected to use generator {0}", ValidateGenerator));
			}
		}

		/// <summary>
		/// Initializes the specified <paramref name="generator"/> as the current generator, for the current thread
		/// </summary>
		/// <remarks>
		/// This is called automatically by the <see cref="Forms.Application"/> when it is constructed
		/// </remarks>
		/// <param name="generator">Generator to set as the current generator</param>
		public static void Initialize(Generator generator)
		{
			if (globalInstance == null)
				globalInstance = generator;
			else
				instance.Value = generator;
		}

		/// <summary>
		/// Initialize the generator with the specified <paramref name="generatorType"/> as the current generator
		/// </summary>
		/// <param name="generatorType">Type of the generator to set as the current generator</param>
		public static void Initialize(string generatorType)
		{
			Initialize(GetGenerator(generatorType));
		}

		/// <summary>
		/// Gets the generator of the specified type
		/// </summary>
		/// <param name="generatorType">Type of the generator to get</param>
		/// <returns>An instance of a Generator of the specified type</returns>
		public static Generator GetGenerator(string generatorType)
		{
			return GetGenerator(generatorType, false);
		}

		static Generator GetGenerator(string generatorType, bool allowNull)
		{
			Type type = Type.GetType(generatorType);
			if (type == null)
			{
				if (allowNull)
					return null;
				throw new EtoException("Generator not found. Are you missing the platform assembly?");
			}
			try
			{
				return (Generator)Activator.CreateInstance(type);
			}
			catch
			{
				if (allowNull)
					return null;
				throw;
			}
		}

		/// <summary>
		/// Add the <paramref name="instantiator"/> for the specified handler type of <typeparamref name="T"/>
		/// </summary>
		/// <param name="instantiator">Instantiator to create an instance of the handler</param>
		/// <typeparam name="T">The handler type to add the instantiator for (usually an interface derived from <see cref="IWidget"/>)</typeparam>
		public void Add<T>(Func<T> instantiator)
			where T: class
		{
			Add(typeof(T), instantiator);
		}

		/// <summary>
		/// Add the specified type and instantiator.
		/// </summary>
		/// <param name="type">Type of the handler (usually an interface derived from <see cref="IWidget"/>)</param>
		/// <param name="instantiator">Instantiator to create an instance of the handler</param>
		public void Add(Type type, Func<object> instantiator)
		{
			instantiatorMap[type] = instantiator;
		}

		/// <summary>
		/// Find the delegate to create instances of the specified <paramref name="type"/>
		/// </summary>
		/// <param name="type">Type of the handler interface to get the instantiator for (usually derived from <see cref="IWidget"/> or another type)</param>
		public Func<object> Find(Type type)
		{
			Func<object> activator;
			if (instantiatorMap.TryGetValue(type, out activator))
				return activator;
			return null;
		}

		/// <summary>
		/// Creates a new instance of the handler of the specified type
		/// </summary>
		/// <param name="type">Type of handler to create</param>
		/// <returns>A new instance of a handler</returns>
		public object Create(Type type)
		{
			try
			{
				var instantiator = Find(type);
				if (instantiator == null)
					throw new HandlerInvalidException(string.Format(CultureInfo.CurrentCulture, "type {0} could not be found in this generator", type.FullName));

				var handler = instantiator();
				OnWidgetCreated(new WidgetCreatedArgs(handler));
				return handler;
			}
			catch (Exception e)
			{
				throw new HandlerInvalidException(string.Format(CultureInfo.CurrentCulture, "Could not create instance of type {0}", type), e);
			}
		}

		/// <summary>
		/// Creates a shared singleton instance of the specified type of <paramref name="type"/>
		/// </summary>
		/// <param name="type">The type of handler to get a shared instance for</param>
		/// <returns>The shared instance of a handler of the given type, or a new instance if not already created</returns>
		public object CreateShared(Type type)
		{
			object instance;
			lock (sharedInstances)
			{
				if (!sharedInstances.TryGetValue(type, out instance))
				{
					instance = Create(type);
					var widget = instance as IWidget;
					if (widget != null)
					{
						widget.Generator = this;
					}
					sharedInstances[type] = instance;
				}
			}
			return instance;
		}

		/// <summary>
		/// Used at the start of your custom threads
		/// </summary>
		/// <returns></returns>
		public virtual IDisposable ThreadStart()
		{
			return null;
		}

		/// <summary>
		/// Gets an object to wrap in the generator's context, when using multiple generators.
		/// </summary>
		/// <remarks>
		/// This sets this generator as current, and reverts back to the previous generator when disposed.
		/// 
		/// This value may be null.
		/// </remarks>
		/// <example>
		/// <code>
		///		using (generator.Context)
		///		{
		///			// do some stuff with the specified generator
		///		}
		/// </code>
		/// </example>
		public IDisposable Context
		{
			get
			{
				if (globalInstance != this)
					return new GeneratorContext(this);
				else
					return null;
			}
		}
	}
}
