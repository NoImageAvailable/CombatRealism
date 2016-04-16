using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace Combat_Realism
{
    public class ITabInjector : SpecialInjector
    {
        #region Methods

        public override bool Inject()
        {
            // get reference to lists of itabs
            var itabs = ThingDefOf.Human.inspectorTabs;
            var itabsResolved = ThingDefOf.Human.inspectorTabsResolved;

            // replace ITab in the unresolved list
            var index = itabs.IndexOf( typeof( ITab_Pawn_Gear ) );
            if ( index != -1 )
            {
                itabs.Remove( typeof( ITab_Pawn_Gear ) );
                itabs.Insert( index, typeof( ITab_Inventory ) );
            }

            // re-resolve all the tabs.
            itabsResolved.Clear();
            foreach ( var tab in itabs )
            {
                itabsResolved.Add( ITabManager.GetSharedInstance( tab ) );
            }

            return true;
        }

        #endregion Methods
    }
}