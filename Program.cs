using System;
using System.Text;

using Mono.Cecil;


namespace ParamsCandidateFinder
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			args = new string[] {
				"/work/monotouch/master/monotouch/src/build/native-32/Xamarin.iOS.dll",
				//"/work/xammac/master/src/build/mobile-32/Xamarin.Mac.dll",
			};

			foreach (var arg in args)
				Process (arg);
			Console.WriteLine ("Found {0} candidates", params_candidates);
		}

		static void Process (string filename)
		{
			Console.WriteLine ("Processing {0}", filename);
			Process (AssemblyDefinition.ReadAssembly (filename));
		}

		static void Process (AssemblyDefinition def)
		{
			def.MainModule.ReadSymbols ();
			foreach (var type in def.MainModule.Types)
				Process (type);
		}

		static void Process (TypeDefinition type)
		{
			if (type.HasNestedTypes)
				foreach (var nestedtype in type.NestedTypes)
					Process (nestedtype);

			if (type.IsNotPublic || type.IsNestedPrivate)
				return;

			if (type.HasCustomAttributes) {
				foreach (var ca in type.CustomAttributes) {
					if (ca.AttributeType.Name == "ProtocolAttribute")
						return;
				}
			}

			if (type.HasMethods)
				foreach (var method in type.Methods)
					Process (method);
		}

		static void Process (MethodDefinition method)
		{
			if (method.IsSpecialName)
				return; // property setters

			if (!method.IsPublic && !method.IsFamily && !method.IsFamilyOrAssembly)
				return; // internal methods

			if (!method.HasParameters)
				return;

			if (method.Parameters.Count == 0)
				return;

			var lastParameter = method.Parameters [method.Parameters.Count - 1];
			var lastParameterType = lastParameter.ParameterType;
			var array = lastParameterType as ArrayType;
			if (array == null)
				return;

			if (array.ElementType.Name == "Byte")
				return; // we don't care about byte arrays

			// we don't care about attributes that are already params
			if (lastParameter.HasCustomAttributes) {
				foreach (var ca in lastParameter.CustomAttributes) {
					if (ca.AttributeType.Name == "ParamArrayAttribute")
						return;
				}
			}

			// We don't care about [Obsolete] methods
			if (method.HasCustomAttributes) {
				foreach (var ca in method.CustomAttributes) {
					if (ca.AttributeType.Name == "ObsoleteAttribute")
						return;
				}
			}

			params_candidates++;

			var sb = new StringBuilder ();
			sb.Append (method.DeclaringType.FullName);
			sb.Append (' ');
			sb.Append (method.Name);
			sb.Append (" (");
			foreach (var p in method.Parameters) {
				if (p.Index > 0)
					sb.Append (", ");
				sb.Append (p.ParameterType.FullName);
			}
			sb.Append (")");

			if (method.HasBody) {
				var seq = method.Body.Instructions [0].SequencePoint;
				if (seq != null) {
					Console.WriteLine ("{0} {1}:{2} ", sb, seq.Document.Url, seq.StartLine - 2);
				} else {
					Console.WriteLine (sb);
				}
			} else {
				Console.WriteLine (sb);
			}

		}
		static int params_candidates;
	}
}
