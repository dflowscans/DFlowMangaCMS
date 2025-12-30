# DFlowScans - URL Routing & Navigation Guide

## 🗺️ Complete URL Map

### Frontend Routes

#### Home Pages
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/` | Home | Index | Home page with featured manga |
| `/Home/Privacy` | Home | Privacy | Privacy policy page |

#### Series Pages
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Series` | Series | Index | Browse all manga series |
| `/Series?search=query` | Series | Index | Search by title/author |
| `/Series?status=Ongoing` | Series | Index | Filter by status |
| `/Series/Detail/1` | Series | Detail | View series details |
| `/Series/ReadChapter/5` | Series | ReadChapter | Read chapter pages |

### Admin Routes

#### Dashboard
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Admin` | Admin | Index | Admin dashboard |

#### Manga Management
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Admin/MangaList` | Admin | MangaList | View all manga |
| `/Admin/MangaList?search=query` | Admin | MangaList | Search manga |
| `/Admin/CreateManga` | Admin | CreateManga | Create new manga |
| `/Admin/EditManga/1` | Admin | EditManga | Edit manga |
| `/Admin/DeleteManga/1` | Admin | DeleteManga | Delete manga |

#### Chapter Management
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Admin/ChapterList/1` | Admin | ChapterList | View chapters for manga |
| `/Admin/CreateChapter/1` | Admin | CreateChapter | Create chapter |
| `/Admin/EditChapter/5` | Admin | EditChapter | Edit chapter |
| `/Admin/DeleteChapter/5` | Admin | DeleteChapter | Delete chapter |

#### Page Management
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Admin/PageList/5` | Admin | PageList | View pages for chapter |
| `/Admin/CreatePage/5` | Admin | CreatePage | Add page |
| `/Admin/EditPage/10` | Admin | EditPage | Edit page |
| `/Admin/DeletePage/10` | Admin | DeletePage | Delete page |

## 📍 Navigation Examples

### From Home Page
```
Home (/)
├─ Series Link → /Series
├─ Admin Link → /Admin
└─ Cards click → /Series/Detail/{id}
```

### From Series Listing
```
Series List (/Series)
├─ Series Card → /Series/Detail/{id}
├─ View All → /Series
├─ Search → /Series?search=term
└─ Filter Status → /Series?status=Ongoing
```

### From Series Detail
```
Series Detail (/Series/Detail/{id})
├─ Read Button → /Series/ReadChapter/{chapterId}
├─ Chapter Item → /Series/ReadChapter/{chapterId}
└─ Back to Series → /Series
```

### From Chapter Reader
```
Chapter Reader (/Series/ReadChapter/{id})
├─ Previous Chapter → /Series/ReadChapter/{prevChapterId}
├─ Next Chapter → /Series/ReadChapter/{nextChapterId}
├─ Chapter List → /Series/Detail/{mangaId}
└─ Chapter Selector → /Series/ReadChapter/{selectedChapterId}
```

### From Admin Dashboard
```
Admin Dashboard (/Admin)
├─ Manage Manga → /Admin/MangaList
├─ View All Chapters → /Admin/ChapterList/{mangaId}
└─ Quick Stats → Dashboard
```

### From Manga List
```
Manga List (/Admin/MangaList)
├─ Create Manga → /Admin/CreateManga
├─ Edit Manga → /Admin/EditManga/{id}
├─ Delete Manga → /Admin/DeleteManga/{id}
├─ View Chapters → /Admin/ChapterList/{id}
├─ Back → /Admin
└─ Search → /Admin/MangaList?search=term
```

### From Chapter List
```
Chapter List (/Admin/ChapterList/{mangaId})
├─ Create Chapter → /Admin/CreateChapter/{mangaId}
├─ Edit Chapter → /Admin/EditChapter/{id}
├─ Delete Chapter → /Admin/DeleteChapter/{id}
├─ Manage Pages → /Admin/PageList/{chapterId}
└─ Back → /Admin/MangaList
```

### From Page List
```
Page List (/Admin/PageList/{chapterId})
├─ Add Page → /Admin/CreatePage/{chapterId}
├─ Edit Page → /Admin/EditPage/{pageId}
├─ Delete Page → /Admin/DeletePage/{pageId}
└─ Back → /Admin/ChapterList/{mangaId}
```

## 🔗 Query Parameters

### Series List Filters
```
/Series
?search=Naruto              # Search by title/author
&status=Ongoing            # Filter by status
&genre=Action              # Filter by genre

Combined:
/Series?search=One Piece&status=Ongoing
```

### Manga List Filters
```
/Admin/MangaList
?search=Bleach             # Search manga

Combined:
/Admin/MangaList?search=My Hero
```

## 📊 Request/Response Flow

### View Manga Series
```
GET /Series/Detail/1
↓
SeriesController.Detail(1)
↓
Loads: Manga + Chapters + Pages
↓
Returns: Detail.cshtml with full data
```

### Create New Manga
```
GET /Admin/CreateManga
↓
Returns: CreateManga.cshtml form

POST /Admin/CreateManga
↓
Validates: ModelState
↓
Saves: New Manga to database
↓
Redirects: /Admin/MangaList
```

### Read Chapter
```
GET /Series/ReadChapter/5
↓
SeriesController.ReadChapter(5)
↓
Increments: ViewCount
↓
Loads: Chapter + Pages
↓
Returns: ReadChapter.cshtml
```

## 🔐 URL Security

### Safe Routes (No Auth Required)
- `/` - Home
- `/Series` - Series listing
- `/Series/Detail/{id}` - Series details
- `/Series/ReadChapter/{id}` - Read chapters

### Admin Routes (Should Require Auth)
- `/Admin/*` - All admin routes
- Recommendation: Implement authentication middleware

## 📱 Mobile-Friendly URLs

All routes are mobile-responsive:
- Hamburger menu for navigation
- Touch-friendly button sizes
- Responsive layouts

## 🔄 Default Route

```csharp
// In Program.cs
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Default: `/` → `/Home/Index`

## 🧭 Navigation Helper Methods

### In Views (Razor)
```html
<!-- Home Link -->
<a asp-controller="Home" asp-action="Index">Home</a>
<!-- Generates: / -->

<!-- Series Link with ID -->
<a asp-controller="Series" asp-action="Detail" asp-route-id="@manga.Id">
    View Series
</a>
<!-- Generates: /Series/Detail/1 -->

<!-- Admin Links -->
<a asp-controller="Admin" asp-action="MangaList">Manage Manga</a>
<!-- Generates: /Admin/MangaList -->

<!-- With Query Strings -->
<a asp-controller="Series" asp-action="Index" asp-route-search="Naruto">
    Search
</a>
<!-- Generates: /Series?search=Naruto -->
```

## 📲 Direct URLs for Quick Access

```
Development:
http://localhost:5000/             (home)
http://localhost:5000/Series       (series)
http://localhost:5000/Admin        (admin dashboard)

HTTPS:
https://localhost:5001/            (home)
https://localhost:5001/Series      (series)
https://localhost:5001/Admin       (admin dashboard)

Production:
https://yourdomain.com/            (home)
https://yourdomain.com/Series      (series)
https://yourdomain.com/Admin       (admin dashboard)
```

## 📑 Page Structure

### Home Page Components
```
/
├─ Navbar (Navigation)
├─ Hero Section (Featured Manga)
├─ Latest Updates Grid
├─ Statistics Cards
└─ Footer
```

### Series List Components
```
/Series
├─ Navbar
├─ Search & Filter Bar
├─ Manga Grid (4/3/2/1 columns)
└─ Footer
```

### Series Detail Components
```
/Series/Detail/{id}
├─ Navbar
├─ Series Header (Cover + Info)
├─ Description
├─ Chapters List (Scrollable)
└─ Footer
```

### Chapter Reader Components
```
/Series/ReadChapter/{id}
├─ Navbar
├─ Chapter Navigation (Prev/Next)
├─ Chapter Selector Dropdown
├─ Page Display (Full width)
├─ Page Navigation (Bottom)
└─ Footer
```

### Admin Dashboard Components
```
/Admin
├─ Navbar
├─ Statistics Cards
├─ Quick Action Buttons
├─ Recent Activity Feed
└─ Footer
```

## 🔍 URL Conventions

| Pattern | Example | Usage |
|---------|---------|-------|
| `/Controller` | `/Series` | List/Index |
| `/Controller/Action/id` | `/Series/Detail/1` | Detail view |
| `/Controller/Action` | `/Admin/CreateManga` | Form action |
| `?param=value` | `?search=One Piece` | Query filters |
| `&param=value` | `&status=Ongoing` | Multiple filters |

---

**Routing Documentation**: 1.0.0  
**Last Updated**: December 2025
