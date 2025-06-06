// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==
/*============================================================
**
** Interface:  ISet
**
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for all generic sets.
**
**
===========================================================*/

using System.Collections.Generic;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Generic collection that guarantees the uniqueness of its elements, as defined
    /// by some comparer. It also supports basic set operations such as Union, Intersection,
    /// Complement and Exclusive Complement.
    /// </summary>
    public interface ISet<T> : ICollection<T>
    {
        //Add ITEM to the set, return true if added, false if duplicate
        new bool Add(T item);

        //Transform this set into its union with the IEnumerable<T> other
        void UnionWith(IEnumerable<T> other);

        //Transform this set into its intersection with the IEnumberable<T> other
        void IntersectWith(IEnumerable<T> other);

        //Transform this set so it contains no elements that are also in other
        void ExceptWith(IEnumerable<T> other);

        //Transform this set so it contains elements initially in this or in other, but not both
        void SymmetricExceptWith(IEnumerable<T> other);

        //Check if this set is a subset of other
        bool IsSubsetOf(IEnumerable<T> other);

        //Check if this set is a superset of other
        bool IsSupersetOf(IEnumerable<T> other);

        //Check if this set is a subset of other, but not the same as it
        bool IsProperSupersetOf(IEnumerable<T> other);

        //Check if this set is a superset of other, but not the same as it
        bool IsProperSubsetOf(IEnumerable<T> other);

        //Check if this set has any elements in common with other
        bool Overlaps(IEnumerable<T> other);

        //Check if this set contains the same and only the same elements as other
        bool SetEquals(IEnumerable<T> other);
    }
}
