// <copyright>
// Copyright by Bircks and Mortar Studio
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock.Data;
using System.Collections.Generic;
using System.Data;
using System;
using System.Diagnostics;
using Rock.Web.Cache;

namespace com.bricksandmortarstudio.BadgeExtensions.PersonProfile.Badge
{

    [Description( "Shows badge if the individual is in a descendant of a group of a specified type." )]
    [Export( typeof( Rock.PersonProfile.BadgeComponent ) )]
    [ExportMetadata( "ComponentName", "In Descendant Of Group Type" )]

    [GroupTypeField( "Group Type", "The type of group to use.", true )]
    [TextField( "Badge Color", "The color of the badge (#ffffff).", true, "#0ab4dd" )]
    public class InDescendantOfGroupType : Rock.PersonProfile.BadgeComponent
    {
        /// <summary>
        /// Renders the specified writer.
        /// </summary>
        /// <param name="badge">The badge.</param>
        /// <param name="writer">The writer.</param>
        public override void Render( PersonBadgeCache badge, System.Web.UI.HtmlTextWriter writer )
        {
            if ( !String.IsNullOrEmpty( GetAttributeValue( badge, "GroupType" ) ) )
            {
                string badgeColor = "#0ab4dd";

                if ( !String.IsNullOrEmpty( GetAttributeValue( badge, "BadgeColor" ) ) )
                {
                    badgeColor = GetAttributeValue( badge, "BadgeColor" );
                }

                Guid groupTypeGuid = GetAttributeValue( badge, "GroupType" ).AsGuid();

                if ( groupTypeGuid != Guid.Empty )
                {
                    var rockContext = new RockContext();

                    // get group type info
                    GroupType groupType = new GroupTypeService( rockContext ).Get( groupTypeGuid );

                    if ( groupType != null )
                    {
                        // get descendant group info
                        GroupService groupService = new GroupService( rockContext );
                        var groupsOfGroupType = groupService.Queryable().Where( g => g.GroupType.Guid == groupTypeGuid );

                        List<int> groupIds = groupsOfGroupType.Select( g => g.Id ).ToList();
                        foreach ( var group in groupsOfGroupType )
                        {
                            groupIds.AddRange( groupService.GetAllDescendents( group.Id ).Select( g => g.Id ) );
                        }

                        // determine if person is in this type of group
                        GroupMemberService groupMemberService = new GroupMemberService( rockContext );

                        GroupMember groupMember = groupMemberService.Queryable( "Person,GroupRole,Group" )
                                                    .Where( t => groupIds.Contains( t.GroupId )
                                                             && t.PersonId == Person.Id
                                                             && t.GroupMemberStatus == GroupMemberStatus.Active
                                                             && t.Group.IsActive )
                                                    .OrderBy( g => g.GroupRole.Order )
                                                    .FirstOrDefault();

                        var badgeHtml = "";
                        var labelText = "";
                        if ( groupMember != null )
                        {
                            badgeHtml = String.Format("<i class='badge-icon {0}' style='color: {1}'></i>", groupType.IconCssClass, badgeColor);
                            labelText = String.Format( "{0} is in a descendant of a {1}", Person.NickName, groupType.Name );
                        }
                        else
                        {
                            badgeHtml = String.Format( "<i class='badge-icon badge-disabled {0}'></i>", groupType.IconCssClass );
                            labelText = String.Format( "{0} is not in a descendant of a {1}", Person.NickName, groupType.Name );
                        }

                        writer.Write( String.Format( @"<div class='badge badge-ingroupoftype badge-id-{0}' data-toggle='tooltip' data-original-title='{1}'>{2}</div>"
                                , badge.Id, labelText,badgeHtml ) );                        
                    }
                }
            }
        }
    }
}

