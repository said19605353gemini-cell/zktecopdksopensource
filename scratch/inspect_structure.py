import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's see what is inside ["tr"], ["en"], ["de"], ["ru"], ["ar"], ["ku"]
# The structure was:
# Line 11: ["tr"]
# Line 150: ["en"]
# Line 287: ["de"]
# Line 424: ["ru"]
# Line 561: ["ar"]
# Line 698: ["ku"]
# Then lines 836-1494 all got appended into ["ku"]!

# Let's inspect where the blocks are:
# Block 1 (835-943): Kurdish (welat, saet)
# Block 2 (945-1053): Arabic (balad, waqt)
# Block 3 (1055-1163): Russian (strana, vremya)
# Block 4 (1165-1273): German (Laender, Zeit)
# Block 5 (1275-1383): English (Country & Clock)
# Block 6 (1385-1494): Turkish (Ulke ve Saat)

print("Block 1 starts at 835, Block 6 ends at 1494. All currently inside ['ku']!")
