using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Helpers;
public static class CockatooDataHelper
{
    /// <summary>
    /// Clone or create a new instance of the class type this method is used on.
    /// </summary>
    public static T JsonClone<T>(this T current)
        where T : class, new()
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(current, BaseService.SerializerOptions), BaseService.SerializerOptions) ?? new();
    }
}
