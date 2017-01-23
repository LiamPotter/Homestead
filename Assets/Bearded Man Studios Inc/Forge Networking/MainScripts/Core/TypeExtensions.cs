using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BeardedManStudios.Network
{
   public static class TypeExtensions
   {
      public static bool IsEnum(this Type type)
      {
#if NetFX_CORE
         return type.GetTypeInfo().IsEnum;
#else
         return type.IsEnum;
#endif
      }

   }
}
