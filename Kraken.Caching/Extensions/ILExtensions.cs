using System.Reflection.Emit;

namespace Kraken.Caching
{
	internal static class ILExtensions
	{
		public static void Emit_Ldc_I4(this ILGenerator il, int num)
		{
			switch (num)
			{
				case 1:
					il.Emit(OpCodes.Ldc_I4_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldc_I4_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldc_I4_3);
					break;
				case 4:
					il.Emit(OpCodes.Ldc_I4_4);
					break;
				case 5:
					il.Emit(OpCodes.Ldc_I4_5);
					break;
				case 6:
					il.Emit(OpCodes.Ldc_I4_6);
					break;
				case 7:
					il.Emit(OpCodes.Ldc_I4_7);
					break;
				case 8:
					il.Emit(OpCodes.Ldc_I4_8);
					break;
				default:
					if (num < 256)
						il.Emit(OpCodes.Ldc_I4_S, (byte)num);
					else
						il.Emit(OpCodes.Ldc_I4, num);
					break;
			}
		}

		public static void Emit_Ldarg(this ILGenerator il, int num)
		{
			switch (num)
			{
				case 1:
					il.Emit(OpCodes.Ldarg_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldarg_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldarg_3);
					break;
				default:
					if (num < 256)
						il.Emit(OpCodes.Ldarg_S, (byte)num);
					else
						il.Emit(OpCodes.Ldarg, num);
					break;
			}
		}

		public static void Emit_Stloc(this ILGenerator il, int num)
		{
			switch (num)
			{
				case 0:
					il.Emit(OpCodes.Stloc_0);
					break;
				case 1:
					il.Emit(OpCodes.Stloc_1);
					break;
				case 2:
					il.Emit(OpCodes.Stloc_2);
					break;
				case 3:
					il.Emit(OpCodes.Stloc_3);
					break;
				default:
					if (num < 256)
						il.Emit(OpCodes.Stloc_S, (byte)num);
					else
						il.Emit(OpCodes.Stloc, num);
					break;
			}
		}

		public static void Emit_Ldloc(this ILGenerator il, int num)
		{
			switch (num)
			{
				case 0:
					il.Emit(OpCodes.Ldloc_0);
					break;
				case 1:
					il.Emit(OpCodes.Ldloc_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldloc_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldloc_3);
					break;
				default:
					if (num < 256)
						il.Emit(OpCodes.Ldloc_S, (byte)num);
					else
						il.Emit(OpCodes.Ldloc, num);
					break;
			}
		}
	}
}
