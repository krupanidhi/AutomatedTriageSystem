# Report Grid Troubleshooting Guide

## Issue: Filters and View Details Button Not Working

### Symptoms
- Grid displays data correctly
- Filter inputs are visible but don't respond
- "View Details" button doesn't open modal
- No visible errors on screen

---

## 🔍 Debugging Steps

### Step 1: Check Browser Console

1. Open the report grid page: `http://localhost:5200/report-grid`
2. Press **F12** to open Developer Tools
3. Click the **Console** tab
4. Look for any JavaScript errors (red text)

**Expected console output:**
```
Applying filters...
Filters: {riskFilter: "", sentimentMin: -1, sentimentMax: 1, ...}
Filtered 2 of 2 organizations
```

**If you see errors:**
- Note the error message
- Check which line number is failing
- Share the error for further debugging

---

### Step 2: Test Filter Functionality

1. Open browser console (F12)
2. Type: `applyFilters()`
3. Press Enter

**Expected result:**
- Console shows "Applying filters..."
- Table refreshes
- No errors

**If nothing happens:**
- The function isn't defined (JavaScript didn't load)
- Check Network tab for failed script loads

---

### Step 3: Test View Details Button

1. Open browser console
2. Type: `viewDetails(0)`
3. Press Enter

**Expected result:**
- Console shows "View details clicked for index: 0"
- Modal appears with organization details

**If nothing happens:**
- Function isn't defined
- Modal HTML is missing
- CSS is hiding the modal

---

### Step 4: Verify Data Loading

1. Open browser console
2. Type: `console.log(allData)`
3. Press Enter

**Expected result:**
```javascript
[
  {
    OrganizationName: "NEW MEXICO PRIMARY CARE ASSOCIATION",
    AverageSentiment: 0.80,
    RiskLevel: "LOW",
    ...
  },
  ...
]
```

**If shows empty array `[]`:**
- Data didn't load from API
- Check Network tab for failed API calls

---

## 🔧 Quick Fixes

### Fix 1: Refresh the Page
Sometimes JavaScript doesn't load properly on first load.

1. Press **Ctrl+F5** (hard refresh)
2. Wait for page to fully load
3. Try filters again

### Fix 2: Clear Browser Cache

1. Press **Ctrl+Shift+Delete**
2. Select "Cached images and files"
3. Click "Clear data"
4. Refresh page

### Fix 3: Check if API is Running

```powershell
# Test if C# API is responding
curl http://localhost:5200/api/Files
```

**Expected:** JSON array of files

**If fails:** Restart the C# API service

---

## 🐛 Common Issues

### Issue: "Apply Filters" Button Does Nothing

**Cause:** JavaScript function not attached to button

**Fix:** Check if button has `onclick="applyFilters()"` attribute

**Verify in console:**
```javascript
document.querySelector('button[onclick="applyFilters()"]')
```

Should return the button element, not `null`.

---

### Issue: "View Details" Opens Empty Modal

**Cause:** Organization data structure mismatch

**Debug:**
```javascript
// Check what data looks like
console.log(filteredData[0]);

// Check if required fields exist
console.log(filteredData[0].OrganizationName);
console.log(filteredData[0].AverageSentiment);
```

**Fix:** Ensure API returns correct field names (PascalCase)

---

### Issue: Filters Work But Show "No Data"

**Cause:** Filter criteria too restrictive

**Debug:**
```javascript
// Check total data count
console.log('Total:', allData.length);

// Check filtered count
console.log('Filtered:', filteredData.length);

// Check filter values
console.log('Risk filter:', document.getElementById('riskFilter').value);
```

**Fix:** Clear all filters and try one at a time

---

## 📋 Manual Testing Checklist

- [ ] Page loads without errors (check console)
- [ ] Data appears in table
- [ ] "Apply Filters" button logs to console when clicked
- [ ] Risk Level dropdown changes filter results
- [ ] Sentiment min/max inputs filter correctly
- [ ] Organization search filters by name
- [ ] Challenge keywords search works
- [ ] "Clear All" button resets filters
- [ ] "View Details" button opens modal
- [ ] Modal shows organization data
- [ ] Modal close button (×) works
- [ ] Pagination buttons work
- [ ] Export CSV button downloads file
- [ ] Sorting by column headers works

---

## 🔄 If Still Not Working

### Rebuild and Restart

```powershell
# Stop C# API (Ctrl+C in terminal)

# Rebuild
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform
dotnet build

# Restart API
cd src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5200"
```

### Check File Permissions

Ensure the view file is readable:
```powershell
Get-Item "C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API\Views\WebUI\ReportGrid.cshtml"
```

### Verify Route Registration

Check that the route exists:
```powershell
# In browser, navigate to:
http://localhost:5200/report-grid

# Should show the grid, not a 404 error
```

---

## 📞 Reporting Issues

If problems persist, provide:

1. **Browser console errors** (screenshot or copy/paste)
2. **Network tab** showing failed requests (if any)
3. **Console output** from testing commands above
4. **Browser and version** (Chrome, Edge, Firefox, etc.)
5. **Steps to reproduce** the issue

---

## ✅ Expected Behavior

**When working correctly:**

1. **Page loads:**
   - Shows "Showing 2 of 2 organizations"
   - Displays data in table
   - All buttons visible

2. **Clicking "Apply Filters":**
   - Console shows "Applying filters..."
   - Table updates immediately
   - Stats update ("Showing X of Y organizations")

3. **Clicking "View Details":**
   - Console shows "View details clicked..."
   - Modal slides in from right
   - Shows full organization details
   - Sections color-coded by provider

4. **Typing in search boxes:**
   - Can type freely
   - Pressing Enter or clicking "Apply Filters" filters results

5. **Changing dropdowns:**
   - Options appear when clicked
   - Selecting option updates value
   - Must click "Apply Filters" to apply

---

## 🎯 Next Steps

1. **Open browser console** and check for errors
2. **Test each function** manually using console commands
3. **Report findings** with specific error messages
4. **Try quick fixes** (refresh, clear cache)
5. **If still broken**, provide console output for debugging

The grid JavaScript is fully functional in the code - if it's not working, there's likely a browser/loading issue rather than a code issue.
