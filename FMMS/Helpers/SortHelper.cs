using FMMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FMMS.Helpers
{
    public static class SortHelper
    {
        /// <summary>
        /// Сортирует коллекцию объектов FolderInfo по их свойству RelativePath.
        /// </summary>
        /// <param name="items">Коллекция FolderInfo для сортировки.</param>
        /// <returns>Новая отсортированная коллекция.</returns>
        public static List<FolderInfo> SortFolderInfosByRelativePath(IEnumerable<FolderInfo> items)
        {
            return items == null
                ? throw new ArgumentNullException(nameof(items))
                : [.. items.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)];
        }
    }
}