import os
import matplotlib.pyplot as plt
import csv

# Define the CSV files to read
directory = "data"
files = os.listdir(directory)

# Filter CSV files
csv_files = [os.path.join(directory, file) for file in files if file.endswith(".csv")]

# Define the fields you want to compare
fields = ["TimeTakenMs", "TimeTakenForDbMs", "TimeTakenFibonnaciMs", "TimeTakenSortMs"]

# Create subplots
fig, axs = plt.subplots(len(fields), 1, figsize=(10, 8))

already_printed_metadata = False
# Loop through each field and plot data from each CSV file
for idx, field in enumerate(fields):
    for csv_file in csv_files:
        # Read data from CSV file
        with open(csv_file, "r") as file:
            metadata = {}
            csv_data = ""
            for line in file:
                if line.startswith("#"):
                    key, value = line.split(":")
                    value = value.replace(",", ".").replace("\n", "").strip()
                    metadata[key.replace("#", "").strip()] = (
                        round(float(value), 2)
                        if value.replace(".", "").isnumeric()
                        else value
                    )
                else:
                    csv_data += line

            reader = csv.DictReader(csv_data.splitlines())
            x = []
            y = []
            for row in reader:
                x.append(int(row["Id"]))
                y.append(
                    float(row[field].replace(",", "."))
                )  # Replace comma with dot for float conversion

            sorted_pairs = sorted(zip(x, y), key=lambda pair: pair[0])
            sorted_x, sorted_y = zip(*sorted_pairs)
            # Plot data
            axs[idx].plot(sorted_x, sorted_y, label=csv_file.split("_")[1])
            # axs[idx].text(len(sorted_x), 0, ", ".join([f"{x}: {y}\n" for x, y in metadata.items()]), fontsize=8)
            if not already_printed_metadata:
                print(f"\n---{metadata['Technology']}---")
                for key, value in metadata.items():
                    if key != "Technology":
                        print(f"{key}: {value}")
    already_printed_metadata = True

    # Set labels and title for each subplot
    axs[idx].set_xlabel("Id")
    axs[idx].set_ylabel(field)
    axs[idx].set_title(f"Comparison of {field}")
    axs[idx].legend()

# Adjust layout
plt.tight_layout()
plt.show()
