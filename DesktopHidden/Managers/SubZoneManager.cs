using System;
using System.Collections.ObjectModel;
using DesktopHidden.Models;
using Windows.Foundation;

namespace DesktopHidden.Managers
{
    public class SubZoneManager
    {
        public ObservableCollection<SubZoneModel> SubZones { get; set; }

        public SubZoneManager()
        {
            SubZones = new ObservableCollection<SubZoneModel>();
        }

        public SubZoneModel AddSubZone(Point position, Size size)
        {
            var newSubZone = new SubZoneModel(position, size);
            SubZones.Add(newSubZone);
            return newSubZone;
        }

        // 其他管理方法，如RemoveSubZone, MoveSubZone, ResizeSubZone, LockSubZone, ToggleContentVisibility等，将在后续实现
    }
}
