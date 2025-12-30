# DFlowScans - Visual Reference & Color Scheme

## 🎨 Dark Theme Color Palette

### Primary Colors
```css
--primary-color: #6366f1       /* Indigo - Main CTA buttons */
--primary-light: #818cf8       /* Light Indigo - Hover states */
--primary-dark: #4f46e5        /* Dark Indigo - Active states */
```

### Background Colors
```css
--bg-dark: #0f172a             /* Main page background */
--bg-darker: #0c0e1a           /* Darker variant */
--bg-secondary: #1e293b        /* Secondary backgrounds */
--bg-tertiary: #334155         /* Tertiary/hover backgrounds */
```

### Text Colors
```css
--text-primary: #f1f5f9        /* Main text */
--text-secondary: #cbd5e1      /* Secondary text */
--text-muted: #94a3b8          /* Muted/disabled text */
```

### Accent Colors
```css
--accent: #ec4899              /* Pink - Highlights/badges */
--success: #10b981             /* Green - Success states */
--border-color: #334155        /* Borders */
```

## 📐 Layout Dimensions

### Navbar
- Height: 60px
- Padding: 1rem
- Position: Sticky (top)
- Z-index: High (stays above content)

### Cards
- Border Radius: 10px
- Padding: 1rem
- Box Shadow: 0 10px 30px rgba(99, 102, 241, 0.2)
- Hover Effect: translateY(-10px)

### Images
- Manga Cards: 3:4 aspect ratio
- Featured Items: Variable height (400px on desktop)
- Pages: 3:4 aspect ratio

### Typography
- Font Family: System fonts (-apple-system, BlinkMacSystemFont, etc.)
- Headers: Font weight 700
- Body: Font weight 400
- Line Height: 1.6

## 📱 Responsive Breakpoints

```css
Large screens (lg): 992px and up
- Full sidebar support
- 3-4 column grids

Tablets (md): 768px to 991px
- 2 column grids
- Adjusted padding

Mobile (sm): 576px to 767px
- 1-2 column grids
- Hamburger menu

Extra small: Below 576px
- Single column
- Full-width elements
- Touch-friendly buttons
```

## 🎯 Component Reference

### Buttons

**Primary (Indigo Gradient)**
```
Background: Linear gradient (primary-color → primary-dark)
Color: White
Padding: 10px 16px
Border Radius: 6px
Hover: Scale & Shadow increase
```

**Secondary (Tertiary Background)**
```
Background: --bg-tertiary
Border: 1px solid --border-color
Color: --text-primary
Hover: Border color changes to primary
```

**Success (Green)**
```
Background: Linear gradient (green colors)
Color: White
Used for: Save/Create operations
```

**Danger (Red)**
```
Background: Linear gradient (red colors)
Color: White
Used for: Delete operations
```

### Cards

**Manga Card Structure**
```
┌─────────────────────┐
│  Cover Image 3:4    │
│  (Hover: Zoom)      │
├─────────────────────┤
│ Title (2 lines max) │
│ Author              │
│ Genre               │
│ View Button         │
└─────────────────────┘
Hover Effect: Glow + Lift
```

**Stats Card Structure**
```
┌──────────────────────────┐
│ Gradient Background      │
│ Icon (Right aligned)     │
│ Number (2rem bold)       │
│ Label (small text)       │
└──────────────────────────┘
```

### Navigation

**Navbar Items**
- Color: --text-secondary
- Hover: --primary-light + slide underline
- Active: --primary-color + underline
- Spacing: 0.5rem gap between items

**Mobile Menu**
- Hamburger icon appears below 992px
- Dropdown navigation
- Full width on mobile

### Badges

**Status Badge**
```
Background: --primary-color
Color: White
Padding: 0.3rem 0.8rem
Border Radius: 20px
Font Size: 0.75rem
Font Weight: 600
```

**Featured Badge**
```
Icon: ⭐
Color: --accent (Pink)
Text: "Featured"
Position: Top-right corner
```

## 🎬 Animation Effects

### Smooth Transitions
- Duration: 0.3s ease
- Applied to: colors, backgrounds, transforms

### Hover Effects
- Card Lift: translateY(-10px)
- Zoom Image: scale(1.1)
- Icon Rotate: rotateZ(5deg)

### Page Load
- Fade in: opacity 0 → 1
- Slide down: translateY(-20px) → 0

## 📊 Grid Layouts

### Home Page Featured
```
Desktop:  1 large banner (full width)
Tablet:   1 large banner (full width)
Mobile:   Adapted height (250px)
```

### Latest Manga Grid
```
Large (lg):    4 columns
Medium (md):   3 columns
Small (sm):    2 columns
Mobile:        1 column
Gap:           1rem
```

### Admin Table
```
Responsive:    Horizontal scroll on mobile
Bordered:      1px solid --border-color
Striped:       Alternating row colors
Header:        --bg-tertiary background
```

## 🔤 Typography Scale

| Usage | Font Size | Weight | Color |
|-------|-----------|--------|-------|
| Page Title | 2.5rem | 700 | Primary Light |
| Section Title | 2rem | 700 | Text Primary |
| Card Title | 1rem | 600 | Text Primary |
| Body Text | 1rem | 400 | Text Secondary |
| Small Text | 0.85rem | 400 | Text Muted |
| Label | 0.9rem | 500 | Text Muted |

## 🌟 Special Effects

### Gradient Text
```css
background: linear-gradient(135deg, var(--primary-light), var(--accent));
-webkit-background-clip: text;
-webkit-text-fill-color: transparent;
background-clip: text;
```

### Glow Effect
```css
box-shadow: 0 20px 50px rgba(99, 102, 241, 0.2);
```

### Gradient Buttons
```css
background: linear-gradient(135deg, #6366f1, #4f46e5);
transition: all 0.3s ease;
```

### Backdrop Blur (Navbar)
```css
backdrop-filter: blur(10px);
```

## 📏 Spacing System

- xs: 0.25rem
- sm: 0.5rem
- md: 1rem
- lg: 1.5rem
- xl: 2rem
- 2xl: 3rem

Used consistently throughout for padding and margins.

## 🎪 Shadow System

| Elevation | Shadow |
|-----------|--------|
| Subtle | 0 2px 8px rgba(0,0,0,0.5) |
| Medium | 0 10px 30px rgba(0,0,0,0.3) |
| Large | 0 20px 50px rgba(99,102,241,0.2) |

## ✅ Accessibility Features

- Semantic HTML structure
- Color contrast compliance (WCAG AA)
- Focus states on interactive elements
- Alt text on all images
- Proper label associations
- Keyboard navigation support

---

**Design System Version**: 1.0.0  
**Last Updated**: December 2025
