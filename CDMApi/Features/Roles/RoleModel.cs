using System;
#nullable enable

namespace CDMApi.Features.Roles
{
    public class RoleModel
    {
        public Guid Id { get; set; }
        public DateTime? SinkCreatedOn { get; set; }
        public DateTime? SinkModifiedOn { get; set; }
        public Int32? isinherited { get; set; }
        public Int32? componentstate { get; set; }
        public Boolean? ismanaged { get; set; }
        public Guid? modifiedonbehalfby { get; set; }
        public String? modifiedonbehalfby_entitytype { get; set; }
        public Guid? businessunitid { get; set; }
        public String? businessunitid_entitytype { get; set; }
        public Guid? createdby { get; set; }
        public String? createdby_entitytype { get; set; }
        public Guid? parentroleid { get; set; }
        public String? parentroleid_entitytype { get; set; }
        public Guid? parentrootroleid { get; set; }
        public String? parentrootroleid_entitytype { get; set; }
        public Guid? roletemplateid { get; set; }
        public String? roletemplateid_entitytype { get; set; }
        public Guid? modifiedby { get; set; }
        public String? modifiedby_entitytype { get; set; }
        public Guid? createdonbehalfby { get; set; }
        public String? createdonbehalfby_entitytype { get; set; }
        public Guid? supportingsolutionid { get; set; }
        public Int32? versionnumber { get; set; }
        public String? createdbyname { get; set; }
        public DateTime? modifiedon { get; set; }
        public String? parentrootroleidname { get; set; }
        public String? modifiedbyname { get; set; }
        public String? canbedeleted { get; set; }
        public String? modifiedonbehalfbyyominame { get; set; }
        public String? createdonbehalfbyyominame { get; set; }
        public String? name { get; set; }
        public Int32? importsequencenumber { get; set; }
        public Guid? organizationid { get; set; }
        public String? modifiedonbehalfbyname { get; set; }
        public Guid? roleidunique { get; set; }
        public DateTime? createdon { get; set; }
        public String? parentroleidname { get; set; }
        public String? modifiedbyyominame { get; set; }
        public DateTime? overriddencreatedon { get; set; }
        public String? createdonbehalfbyname { get; set; }
        public String? organizationidname { get; set; }
        public String? iscustomizable { get; set; }
        public Guid? solutionid { get; set; }
        public Guid? roleid { get; set; }
        public String? businessunitidname { get; set; }
        public String? createdbyyominame { get; set; }
        public DateTime? overwritetime { get; set; }
    }
}