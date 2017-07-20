#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace FamilyBoundingBox
{
  [Transaction( TransactionMode.ReadOnly )]
  public class Command : IExternalCommand
  {
    /// <summary>
    /// Computes the effective 'BoundingBoxXYZ' of the 
    /// whole family, including all combined and 
    /// individual forms.
    /// </summary>
    /// <param name="document">The Revit document that 
    /// contains the family.</param>
    /// <returns>The effective 'BoundingBoxXYZ' of the 
    /// whole family.</returns>
    private static BoundingBoxXYZ ComputeFamilyBoundingBoxXyz(
      Document document )
    {
      BoundingBoxXYZ familyBoundingBoxXyz = null;

      ISet<ElementId> genericFormExclusionSet
        = new HashSet<ElementId>();

      familyBoundingBoxXyz
        = MergeGeomCombinationBoundingBoxXyz( document, 
          familyBoundingBoxXyz, 
          genericFormExclusionSet );

      familyBoundingBoxXyz
        = MergeSolidBoundingBoxXyz( document, 
          familyBoundingBoxXyz, 
          genericFormExclusionSet );
      
      familyBoundingBoxXyz
        = MergeFamilyInstanceBoundingBoxXyz( document,
          familyBoundingBoxXyz);

      return familyBoundingBoxXyz;
    }

    /// <summary>
    /// Merge <paramref name="boundingBoxXyz"/> with the 
    /// 'BoundingBoxXYZ's of all 'GeomCombination's in 
    /// <paramref name="document"/> into a new 'BoundingBoxXYZ'. 
    /// Collect all members of the 'GeomCombination's
    /// found into <paramref name="geomCombinationMembers"/>.
    /// </summary>
    /// <param name="document">The Revit 'Document' to search 
    /// for all 'GeomCombination's.</param>
    /// <param name="boundingBoxXyz">The 'BoundingBoxXYZ' to merge with.</param>
    /// <param name="geomCombinationMembers">A 'HashSet' to collect all of the
    /// 'GeomCombination' members that form the 'GeomCombination'.</param>
    /// <returns>The new merged 'BoundingBoxXYZ' of
    /// <paramref name="boundingBoxXyz"/> and all 'GeomCombination's
    /// in <paramref name="document"/></returns>
    private static BoundingBoxXYZ MergeGeomCombinationBoundingBoxXyz(
      Document document,
      BoundingBoxXYZ boundingBoxXyz,
      ISet<ElementId> geomCombinationMembers )
    {
      BoundingBoxXYZ mergedResult = boundingBoxXyz;

      FilteredElementCollector geomCombinationCollector
        = new FilteredElementCollector( document )
          .OfClass( typeof( GeomCombination ) );

      foreach( GeomCombination geomCombination in geomCombinationCollector )
      {
        if( geomCombinationMembers != null )
        {
          foreach( GenericForm genericForm in geomCombination.AllMembers )
          {
            geomCombinationMembers.Add( genericForm.Id );
          }
        }

        BoundingBoxXYZ geomCombinationBoundingBox 
          = geomCombination.get_BoundingBox( null );

        if ( geomCombinationBoundingBox == null )
        {
          continue;
        }

        if( mergedResult == null )
        {
          mergedResult = new BoundingBoxXYZ();
          mergedResult.Min = geomCombinationBoundingBox.Min;
          mergedResult.Max = geomCombinationBoundingBox.Max;
          continue;
        }

        mergedResult = MergeBoundingBoxXyz(
          mergedResult, geomCombinationBoundingBox );
      }

      return mergedResult;
    }

    /// <summary>
    /// Merge <paramref name="boundingBoxXyz"/> with the 'BoundingBoxXYZ's of
    /// all 'GenericForm's in <paramref name="document"/> that are solid into
    /// a new 'BoundingBoxXYZ'.
    /// Exclude all 'GenericForm's in
    /// <paramref name="genericFormExclusionSet"/> from being found in
    /// <paramref name="document"/>.
    /// </summary>
    /// <param name="document">The Revit 'Document' to search for all
    /// 'GenericForm's excluding the ones in
    /// <paramref name="genericFormExclusionSet"/>.</param>
    /// <param name="boundingBoxXyz">The 'BoundingBoxXYZ' to merge with.</param>
    /// <param name="genericFormExclusionSet">A 'HashSet' of all the
    /// 'GenericForm's to exclude from being merged with in
    /// <paramref name="document"/>.</param>
    /// <returns>The new merged 'BoundingBoxXYZ' of
    /// <paramref name="boundingBoxXyz"/> and all 'GenericForm's excluding
    /// the ones in <paramref name="genericFormExclusionSet"/>
    /// in <paramref name="document"/></returns>
    private static BoundingBoxXYZ MergeSolidBoundingBoxXyz(
      Document document,
      BoundingBoxXYZ boundingBoxXyz,
      ISet<ElementId> genericFormExclusionSet )
    {
      BoundingBoxXYZ mergedResult = boundingBoxXyz;

      FilteredElementCollector genericFormCollector
        = new FilteredElementCollector( document )
          .OfClass( typeof( GenericForm ) );

      if( genericFormExclusionSet != null
        && genericFormExclusionSet.Any() )
      {
        genericFormCollector.Excluding(
          genericFormExclusionSet );
      }

      foreach( GenericForm solid in
        genericFormCollector
          .Cast<GenericForm>()
          .Where( genericForm => genericForm.IsSolid ) )
      {
        BoundingBoxXYZ solidBoundingBox
          = solid.get_BoundingBox( null );

        if ( solidBoundingBox == null )
        {
          continue;
        }

        if( mergedResult == null )
        {
          mergedResult = new BoundingBoxXYZ();
          mergedResult.Min = solidBoundingBox.Min;
          mergedResult.Max = solidBoundingBox.Max;
          continue;
        }

        mergedResult = MergeBoundingBoxXyz(
          mergedResult, solidBoundingBox );
      }

      return mergedResult;
    }

    /// <summary>
    /// Merge <paramref name="boundingBoxXyz"/> with the 'BoundingBoxXYZ's of
    /// all 'FamilyInstance's in <paramref name="document"/> into a new
    /// 'BoundingBoxXYZ'.
    /// </summary>
    /// <param name="document">The Revit 'Document' to search for all
    /// 'FamilyInstance's.
    /// <param name="boundingBoxXyz">The 'BoundingBoxXYZ' to merge with.</param>
    /// <returns>The new merged 'BoundingBoxXYZ' of
    /// <paramref name="boundingBoxXyz"/> and all 'FamilyInstance's
    /// in <paramref name="document"/></returns>
    private static BoundingBoxXYZ MergeFamilyInstanceBoundingBoxXyz(
      Document document,
      BoundingBoxXYZ boundingBoxXyz )
    {
      BoundingBoxXYZ mergedResult = boundingBoxXyz;

      FilteredElementCollector familyInstanceCollector
        = new FilteredElementCollector( document )
        .OfClass( typeof( FamilyInstance ) );

      foreach ( FamilyInstance familyInstance in familyInstanceCollector )
      {
        BoundingBoxXYZ familyInstanceBoundingBox
          = ComputeFamilyInstanceBoundingBoxXyz( familyInstance );

        if ( familyInstanceBoundingBox == null )
        {
          continue;
        }

        if ( mergedResult == null )
        {
          mergedResult = new BoundingBoxXYZ();
          mergedResult.Min = familyInstanceBoundingBox.Min;
          mergedResult.Max = familyInstanceBoundingBox.Max;
          continue;
        }

        mergedResult = MergeBoundingBoxXyz(
          mergedResult, familyInstanceBoundingBox );
      }

      return mergedResult;
    }

    /// <summary>
    /// Compute the 'BoundingBoxXYZ' of <paramref name="familyInstance"/>.
    /// 
    /// Required because 'FamilyInstance.get_BoundingBox( null )' does not give
    /// a tight 'BoundingBoxXYZ'.
    /// </summary>
    /// <param name="familyInstance">The 'FamilyInstance' to compute the
    /// 'BoundingBoxXYZ' of.</param>
    /// <returns>The 'BoundingBoxXYZ' of
    /// <paramref name="familyInstance"/></returns>
    private static BoundingBoxXYZ ComputeFamilyInstanceBoundingBoxXyz(
      FamilyInstance familyInstance )
    {
      List<XYZ> points = new List<XYZ>();

      Options options = new Options();
      GeometryElement geometryElement
        = familyInstance.get_Geometry( options );
      foreach ( GeometryInstance geometryInstance in geometryElement )
      {
        foreach ( GeometryObject geometryObject in
          geometryInstance.GetInstanceGeometry() )
        {
          Curve curve = geometryObject as Curve;
          if ( curve != null )
          {
            points.AddRange( curve.Tessellate() );
          }

          Solid solid = geometryObject as Solid;
          if ( solid != null )
          {
            foreach ( Edge edge in solid.Edges )
            {
              points.AddRange( edge.Tessellate() );
            }
          }
        }
      }

      BoundingBoxXYZ boundingBoxXYZ
        = ComputeBoundingBoxXYZFromPoints( points );

      return boundingBoxXYZ;
    }

    /// <summary>
    /// Compute the 'BoundingBoxXYZ' of <paramref name="points"/>.
    /// </summary>
    /// <param name="points">The 'XYZ' to compute the 'BoundingBoxXYZ'
    /// of.</param>
    /// <returns>The 'BoundingBoxXYZ' of <paramref name="points"/>.</returns>
    private static BoundingBoxXYZ ComputeBoundingBoxXYZFromPoints(
      IEnumerable<XYZ> points)
    {
      BoundingBoxXYZ boundingBoxXYZ = null;
      if ( points.Any() )
      {
        double minX = Double.PositiveInfinity;
        double minY = Double.PositiveInfinity;
        double minZ = Double.PositiveInfinity;
        double maxX = Double.NegativeInfinity;
        double maxY = Double.NegativeInfinity;
        double maxZ = Double.NegativeInfinity;

        foreach ( XYZ point in points )
        {
          minX = Math.Min( minX, point.X );
          minY = Math.Min( minY, point.Y );
          minZ = Math.Min( minZ, point.Z );
          maxX = Math.Max( maxX, point.X );
          maxY = Math.Max( maxY, point.Y );
          maxZ = Math.Max( maxZ, point.Z );
        }

        boundingBoxXYZ = new BoundingBoxXYZ();
        boundingBoxXYZ.Min = new XYZ( minX, minY, minZ );
        boundingBoxXYZ.Max = new XYZ( maxX, maxY, maxZ );
      }
      return boundingBoxXYZ;
    }

    /// <summary>
    /// Merge <paramref name="boundingBoxXyz0"/> and
    /// <paramref name="boundingBoxXyz1"/> into a new 'BoundingBoxXYZ'.
    /// </summary>
    /// <param name="boundingBoxXyz0">A 'BoundingBoxXYZ' to merge</param>
    /// <param name="boundingBoxXyz1">A 'BoundingBoxXYZ' to merge</param>
    /// <returns>The new merged 'BoundingBoxXYZ'.</returns>
    static BoundingBoxXYZ MergeBoundingBoxXyz(
      BoundingBoxXYZ boundingBoxXyz0,
      BoundingBoxXYZ boundingBoxXyz1 )
    {
      BoundingBoxXYZ mergedResult = new BoundingBoxXYZ();

      mergedResult.Min = new XYZ(
        Math.Min( boundingBoxXyz0.Min.X, boundingBoxXyz1.Min.X ),
        Math.Min( boundingBoxXyz0.Min.Y, boundingBoxXyz1.Min.Y ),
        Math.Min( boundingBoxXyz0.Min.Z, boundingBoxXyz1.Min.Z ) );

      mergedResult.Max = new XYZ(
        Math.Max( boundingBoxXyz0.Max.X, boundingBoxXyz1.Max.X ),
        Math.Max( boundingBoxXyz0.Max.Y, boundingBoxXyz1.Max.Y ),
        Math.Max( boundingBoxXyz0.Max.Z, boundingBoxXyz1.Max.Z ) );

      return mergedResult;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      BoundingBoxXYZ bb = ComputeFamilyBoundingBoxXyz( doc );

      return Result.Succeeded;
    }
  }
}
