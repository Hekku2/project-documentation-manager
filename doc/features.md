# Features

This document describes the main features of the software.

## Feature goals

### Master data markdown

Users can create markdown document which takes it's content from other markdown documents. Purpose of this is to have a one master source for information, but it should be usable in other places.

Following example explains the idea

Inputs

File: windows-features.mdext
```markdown
 * windows feature
 <MarkDownExtension operation="insert" file="common-features.mdsrc" />
```

File: ubuntu-features.mdext
```markdown
 * linux feature
 <MarkDownExtension operation="insert" file="common-features.mdsrc" />
```

File: common-features.mdsrc
```markdown
 * common feature
```

Outputs:
File: windows-features.md
```markdown
 * windows feature
 * common feature
```

File: ubuntu-features.md
```markdown
 * linux feature
 * common feature
```
