// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Objects
{
    public partial class DxfSunStudy
    {
        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            bool seenVersion = false;
            bool readingHours = false;
            int julianDay = -1;
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                switch (pair.Code)
                {
                    case 1:
                        this.SunSetupName = (pair.StringValue);
                        break;
                    case 2:
                        this.Description = (pair.StringValue);
                        break;
                    case 3:
                        this.SheetSetName = (pair.StringValue);
                        break;
                    case 4:
                        this.SheetSubsetName = (pair.StringValue);
                        break;
                    case 40:
                        this.Spacing = (pair.DoubleValue);
                        break;
                    case 70:
                        this.OutputType = (pair.ShortValue);
                        break;
                    case 73:
                        readingHours = true;
                        break;
                    case 74:
                        this.ShadePlotType = (pair.ShortValue);
                        break;
                    case 75:
                        this.ViewportsPerPage = (pair.ShortValue);
                        break;
                    case 76:
                        this.ViewportDistributionRowCount = (pair.ShortValue);
                        break;
                    case 77:
                        this.ViewportDistributionColumnCount = (pair.ShortValue);
                        break;
                    case 90:
                        if (!seenVersion)
                        {
                            this.Version = pair.IntegerValue;
                            seenVersion = true;
                        }
                        else
                        {
                            // after the version, 90 pairs come in julianDay/secondsPastMidnight duals
                            if (julianDay == -1)
                            {
                                julianDay = pair.IntegerValue;
                            }
                            else
                            {
                                var date = DxfCommonConverters.DateDouble(julianDay);
                                date = date.AddSeconds(pair.IntegerValue);
                                this.Dates.Add(date);
                                julianDay = -1;
                            }
                        }
                        break;
                    case 93:
                        this.StartTime_SecondsPastMidnight = (pair.IntegerValue);
                        break;
                    case 94:
                        this.EndTime_SecondsPastMidnight = (pair.IntegerValue);
                        break;
                    case 95:
                        this.IntervalInSeconds = (pair.IntegerValue);
                        break;
                    case 290:
                        if (!readingHours)
                        {
                            this.UseSubset = (pair.BoolValue);
                            readingHours = true;
                        }
                        else
                        {
                            this.Hours.Add(pair.ShortValue);
                        }
                        break;
                    case 291:
                        this.SelectDatesFromCalendar = (pair.BoolValue);
                        break;
                    case 292:
                        this.SelectRangeOfDates = (pair.BoolValue);
                        break;
                    case 293:
                        this.LockViewports = (pair.BoolValue);
                        break;
                    case 294:
                        this.LabelViewports = (pair.BoolValue);
                        break;
                    case 340:
                        this.PageSetupWizardPointer = UIntHandle(pair.StringValue);
                        break;
                    case 341:
                        this.ViewPointer = UIntHandle(pair.StringValue);
                        break;
                    case 342:
                        this.VisualStyleID = UIntHandle(pair.StringValue);
                        break;
                    case 343:
                        this.TextStyleID = UIntHandle(pair.StringValue);
                        break;
                    default:
                        if (!base.TrySetPair(pair))
                        {
                            ExcessCodePairs.Add(pair);
                        }
                        break;
                }

                buffer.Advance();
            }

            return PostParse();
        }
    }
}
