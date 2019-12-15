using System;
#nullable enable

namespace CDMApi.Features.Faxes
{
    public class FaxModel
    {
        public Guid Id { get; set; }
        public DateTime? SinkCreatedOn { get; set; }
        public DateTime? SinkModifiedOn { get; set; }
        public Int32? statecode { get; set; }
        public Int32? statuscode { get; set; }
        public Int32? prioritycode { get; set; }
        public Boolean? directioncode { get; set; }
        public Boolean? isworkflowcreated { get; set; }
        public Boolean? isbilled { get; set; }
        public Boolean? isregularactivity { get; set; }
        public Guid? modifiedonbehalfby { get; set; }
        public String? modifiedonbehalfby_entitytype { get; set; }
        public Guid? transactioncurrencyid { get; set; }
        public String? transactioncurrencyid_entitytype { get; set; }
        public Guid? slaid { get; set; }
        public String? slaid_entitytype { get; set; }
        public Guid? owningbusinessunit { get; set; }
        public String? owningbusinessunit_entitytype { get; set; }
        public Guid? modifiedby { get; set; }
        public String? modifiedby_entitytype { get; set; }
        public Guid? owninguser { get; set; }
        public String? owninguser_entitytype { get; set; }
        public Guid? slainvokedid { get; set; }
        public String? slainvokedid_entitytype { get; set; }
        public Guid? createdby { get; set; }
        public String? createdby_entitytype { get; set; }
        public Guid? createdonbehalfby { get; set; }
        public String? createdonbehalfby_entitytype { get; set; }
        public Guid? regardingobjectid { get; set; }
        public String? regardingobjectid_entitytype { get; set; }
        public Guid? owningteam { get; set; }
        public String? owningteam_entitytype { get; set; }
        public Guid? ownerid { get; set; }
        public String? ownerid_entitytype { get; set; }
        public Guid? to { get; set; }
        public Guid? from { get; set; }
        public DateTime? scheduledend { get; set; }
        public String? createdonbehalfbyyominame { get; set; }
        public Int32? onholdtime { get; set; }
        public DateTime? lastonholdtime { get; set; }
        public String? faxnumber { get; set; }
        public Guid? processid { get; set; }
        public Guid? subscriptionid { get; set; }
        public Int32? actualdurationminutes { get; set; }
        public DateTime? scheduledstart { get; set; }
        public String? modifiedbyyominame { get; set; }
        public DateTime? overriddencreatedon { get; set; }
        public String? slainvokedidname { get; set; }
        public String? tsid { get; set; }
        public DateTime? modifiedon { get; set; }
        public Int32? timezoneruleversionnumber { get; set; }
        public String? modifiedonbehalfbyname { get; set; }
        public String? activitytypecode { get; set; }
        public Int32? versionnumber { get; set; }
        public Int32? numberofpages { get; set; }
        public String? createdbyname { get; set; }
        public String? billingcode { get; set; }
        public String? createdbyyominame { get; set; }
        public String? regardingobjectidname { get; set; }
        public String? transactioncurrencyidname { get; set; }
        public Int32? importsequencenumber { get; set; }
        public String? owneridname { get; set; }
        public DateTime? createdon { get; set; }
        public Int32? utcconversiontimezonecode { get; set; }
        public Int32? scheduleddurationminutes { get; set; }
        public String? category { get; set; }
        public String? subcategory { get; set; }
        public DateTime? actualend { get; set; }
        public String? createdonbehalfbyname { get; set; }
        public String? modifiedbyname { get; set; }
        public String? description { get; set; }
        public Guid? activityid { get; set; }
        public String? regardingobjectidyominame { get; set; }
        public String? coverpagename { get; set; }
        public Guid? stageid { get; set; }
        public String? subject { get; set; }
        public Decimal? exchangerate { get; set; }
        public String? modifiedonbehalfbyyominame { get; set; }
        public DateTime? sortdate { get; set; }
        public String? slaname { get; set; }
        public String? owneridtype { get; set; }
        public String? traversedpath { get; set; }
        public DateTime? actualstart { get; set; }
        public String? regardingobjecttypecode { get; set; }
        public String? owneridyominame { get; set; }
    }
}
